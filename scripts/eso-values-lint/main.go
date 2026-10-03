// Command eso-values-lint guards the two renderers that run over files a
// kustomize `configMapGenerator` slurps whole, and that neither `kustomize
// build`, nor `helm template`, nor `flate test` exercises.
//
// Both have caused a CrowdSec outage (equestria-cluster#2977, #2980 /
// vault#111). The failure mode is the same both times: a *comment* in a
// generated file is not a comment by the time the file is rendered.
//
//	stage 1  kustomize configMapGenerator
//	         reads the file as one opaque string. Comments are part of it.
//	stage 2  Flux postBuild envsubst
//	         runs over the whole built output, so a `${NAME}` anywhere in that
//	         string -- comments included -- is substituted. Undefined names
//	         expand to the empty string with no error and no log line.
//	stage 3  external-secrets `templateAs: Values`
//	         runs the whole string through Go text/template. A `{{` inside a
//	         comment is a live action: bad syntax fails the ExternalSecret
//	         outright (no Secret at all, so any HelmRelease pointing at it
//	         cannot resolve its values), and good syntax silently expands a
//	         real secret into a comment.
//
// Checks, in the order they are reported:
//
//	ESO-PARSE   file reached by `templateAs: Values` does not parse as a Go
//	            text/template. This is the exact error external-secrets
//	            reports, produced by the same parser.
//	ESO-EXEC    it parses but will not execute.
//	ESO-COMMENT a comment line in such a file contains `{{`.
//	ENVSUBST    a comment line in ANY generated file contains a LIVE `${`.
//	            `$$` is envsubst's escape, so `$${NAME}` is fine; the run of
//	            dollars before the brace has to be even.
//
// KNOWN LIMITS, so nobody disables this over a surprise:
//
//   - The two comment checks are line-based: any line whose first non-space
//     character is `#` is treated as a comment. Inside a YAML block scalar
//     such a line is content, not a comment, so a shell snippet embedded in
//     values that legitimately wants `${NAME}` on a `#` line will be flagged.
//     Escape it (`$${NAME}`) or reword. As of writing there is no such case in
//     either cluster repo.
//   - ESO-EXEC executes against a map synthesised from the template's own
//     field references, so it proves the template RUNS. It cannot prove a key
//     exists in the provider -- only external-secrets, holding the live
//     secret, can do that.
//   - `secretGenerator` is not scanned. Neither repo uses one; add it here if
//     that changes, since stage 2 applies to those files identically.
//
// Usage: go run ./scripts/eso-values-lint [root ...]   (default: ./kubernetes)
package main

import (
	"errors"
	"fmt"
	"io"
	"io/fs"
	"os"
	"path/filepath"
	"regexp"
	"slices"
	"strings"
	"text/template"
	"text/template/parse"

	"gopkg.in/yaml.v3"
)

type finding struct {
	check string
	file  string
	line  int
	msg   string
}

type generated struct {
	// path of the file on disk
	path string
	// ConfigMap name as written in kustomization.yaml, e.g. "${APP}-values".
	// Left unsubstituted on purpose: the ExternalSecret spells it the same way.
	cmName string
	// key the file lands under inside the ConfigMap
	key string
	// kustomization.yaml that generates it
	from string
}

// check identifiers, as they appear in output
const (
	checkParse    = "ESO-PARSE"
	checkExec     = "ESO-EXEC"
	checkComment  = "ESO-COMMENT"
	checkEnvsubst = "ENVSUBST"
)

// exit codes
const (
	exitOK      = 0
	exitFinding = 1
	exitFailure = 2 // the linter itself could not run
)

const esoCommentAdvice = "external-secrets templates this whole file, so this comment is " +
	"executable code. Describe the sequence in words, or move the value out of the templated file."

// report is the outcome of a run: what was inspected, and what was wrong.
type report struct {
	findings         []finding
	checkedGenerated int
	checkedTemplates int
}

func main() {
	roots := os.Args[1:]
	if len(roots) == 0 {
		roots = []string{"kubernetes"}
	}
	os.Exit(run(roots, os.Stdout, os.Stderr))
}

func run(roots []string, stdout, stderr io.Writer) int {
	rep, err := scan(roots)
	if err != nil {
		fmt.Fprintf(stderr, "eso-values-lint: %v\n", err)
		return exitFailure
	}

	slices.SortFunc(rep.findings, func(a, b finding) int {
		if c := strings.Compare(a.file, b.file); c != 0 {
			return c
		}
		return a.line - b.line
	})

	for _, f := range rep.findings {
		if f.line > 0 {
			fmt.Fprintf(stdout, "%s:%d: [%s] %s\n", f.file, f.line, f.check, f.msg)
		} else {
			fmt.Fprintf(stdout, "%s: [%s] %s\n", f.file, f.check, f.msg)
		}
	}
	fmt.Fprintf(stdout, "\neso-values-lint: %d generated file(s) checked, %d of them rendered by "+
		"external-secrets `templateAs: Values`, %d finding(s)\n",
		rep.checkedGenerated, rep.checkedTemplates, len(rep.findings))

	if len(rep.findings) > 0 {
		return exitFinding
	}
	return exitOK
}

func scan(roots []string) (report, error) {
	var rep report
	for _, root := range roots {
		dirs, err := kustomizationDirs(root)
		if err != nil {
			return rep, err
		}
		for _, dir := range dirs {
			if err := scanDir(dir, &rep); err != nil {
				return rep, fmt.Errorf("%s: %w", dir, err)
			}
		}
	}
	return rep, nil
}

func scanDir(dir string, rep *report) error {
	gens, err := generatedFiles(dir)
	if err != nil || len(gens) == 0 {
		return err
	}
	templated, err := esoTemplatedKeys(dir)
	if err != nil {
		return err
	}

	for _, g := range gens {
		// #nosec G304 -- the path comes from the repo's own kustomization.yaml,
		// which is the thing being linted. There is no untrusted input here.
		src, err := os.ReadFile(g.path)
		if err != nil {
			return err
		}
		rep.checkedGenerated++
		text := string(src)

		// ENVSUBST applies to every generated file: stage 2 does not care
		// whether stage 3 exists.
		rep.findings = append(rep.findings, unescapedSubstitution(g.path, text)...)

		if !templated[g.cmName+"\x00"+g.key] {
			continue
		}
		rep.checkedTemplates++
		rep.findings = append(rep.findings,
			commentSequence(g.path, text, "{{", checkComment, esoCommentAdvice)...)
		rep.findings = append(rep.findings, goTemplate(g.path, text)...)
	}
	return nil
}

// ---------------------------------------------------------------- discovery

func kustomizationDirs(root string) ([]string, error) {
	var dirs []string
	err := filepath.WalkDir(root, func(p string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}
		if d.IsDir() {
			return nil
		}
		if d.Name() == "kustomization.yaml" || d.Name() == "kustomization.yml" {
			dirs = append(dirs, filepath.Dir(p))
		}
		return nil
	})
	return dirs, err
}

// Minimal shapes for the two documents this tool has to understand. Only the
// fields that matter are modelled; everything else is ignored on decode.

type configMapGenerator struct {
	Name  string   `yaml:"name"`
	Files []string `yaml:"files"`
}

type kustomizationDoc struct {
	ConfigMapGenerator []configMapGenerator `yaml:"configMapGenerator"`
}

type templateItem struct {
	Key        string `yaml:"key"`
	TemplateAs string `yaml:"templateAs"`
}

type templateConfigMap struct {
	Name  string         `yaml:"name"`
	Items []templateItem `yaml:"items"`
}

type templateFromEntry struct {
	ConfigMap templateConfigMap `yaml:"configMap"`
}

type secretTemplate struct {
	TemplateFrom []templateFromEntry `yaml:"templateFrom"`
}

type secretTarget struct {
	Template secretTemplate `yaml:"template"`
}

type externalSecretSpec struct {
	Target secretTarget `yaml:"target"`
}

type externalSecretDoc struct {
	Kind string             `yaml:"kind"`
	Spec externalSecretSpec `yaml:"spec"`
}

func generatedFiles(dir string) ([]generated, error) {
	var out []generated
	for _, name := range []string{"kustomization.yaml", "kustomization.yml"} {
		p := filepath.Join(dir, name)
		// #nosec G304 -- p is a kustomization.yaml under a root given on argv.
		raw, err := os.ReadFile(p)
		if errors.Is(err, fs.ErrNotExist) {
			continue
		}
		if err != nil {
			return nil, err
		}
		var doc kustomizationDoc
		if err := yaml.Unmarshal(raw, &doc); err != nil {
			// A kustomization we cannot parse is not this tool's problem;
			// flate/kustomize will report it far better than we can.
			continue
		}
		for _, gen := range doc.ConfigMapGenerator {
			for _, entry := range gen.Files {
				key, rel := entry, entry
				if i := strings.Index(entry, "="); i >= 0 {
					key, rel = entry[:i], entry[i+1:]
				} else {
					key = filepath.Base(entry)
				}
				fp := filepath.Clean(filepath.Join(dir, rel))
				if _, err := os.Stat(fp); err != nil {
					continue
				}
				out = append(out, generated{path: fp, cmName: gen.Name, key: key, from: p})
			}
		}
	}
	return out, nil
}

// esoTemplatedKeys returns the set of "<configMap name>\x00<key>" pairs that an
// ExternalSecret in dir renders with `templateAs: Values`.
func esoTemplatedKeys(dir string) (map[string]bool, error) {
	set := map[string]bool{}
	entries, err := os.ReadDir(dir)
	if err != nil {
		return nil, err
	}
	for _, e := range entries {
		if e.IsDir() {
			continue
		}
		ext := filepath.Ext(e.Name())
		if ext != ".yaml" && ext != ".yml" {
			continue
		}
		// #nosec G304 -- dir is a repo path supplied on argv.
		raw, err := os.ReadFile(filepath.Join(dir, e.Name()))
		if err != nil {
			return nil, err
		}
		dec := yaml.NewDecoder(strings.NewReader(string(raw)))
		for {
			var doc externalSecretDoc
			if err := dec.Decode(&doc); err != nil {
				break // EOF, or a document this tool does not model
			}
			if doc.Kind != "ExternalSecret" {
				continue
			}
			for _, tf := range doc.Spec.Target.Template.TemplateFrom {
				for _, item := range tf.ConfigMap.Items {
					if item.TemplateAs == "Values" {
						set[tf.ConfigMap.Name+"\x00"+item.Key] = true
					}
				}
			}
		}
	}
	return set, nil
}

// ------------------------------------------------------------------- checks

// commentSequence reports `seq` appearing on a line whose first non-space
// character is `#`. JSON has no comments, so it is skipped.
func commentSequence(path, text, seq, check, msg string) []finding {
	var out []finding
	forEachCommentLine(path, text, func(i int, line string) {
		if strings.Contains(line, seq) {
			out = append(out, finding{check: check, file: path, line: i + 1,
				msg: fmt.Sprintf("comment contains %q. %s", seq, msg)})
		}
	})
	return out
}

// unescapedSubstitution reports a LIVE `${...}` in a comment. `$$` is
// envsubst's escape for a literal dollar, so `$${NAME}` survives verbatim and
// is fine; the run of dollars immediately before the brace has to be even for
// that to hold.
func unescapedSubstitution(path, text string) []finding {
	var out []finding
	forEachCommentLine(path, text, func(i int, line string) {
		for j := 1; j < len(line); j++ {
			if line[j] != '{' || line[j-1] != '$' {
				continue
			}
			dollars := 0
			for k := j - 1; k >= 0 && line[k] == '$'; k-- {
				dollars++
			}
			if dollars%2 == 1 {
				out = append(out, finding{check: checkEnvsubst, file: path, line: i + 1,
					msg: "comment contains a live substitution. Flux postBuild envsubst " +
						"expands it even inside a comment -- an undefined name expands to " +
						"nothing and eats the text around it, and a defined one bakes a real " +
						"cluster value into the ConfigMap. Double the dollar sign to escape " +
						"it, or describe the sequence in words."})
				return
			}
		}
	})
	return out
}

func forEachCommentLine(path, text string, fn func(i int, line string)) {
	if ext := filepath.Ext(path); ext == ".json" {
		return // JSON has no comments
	}
	for i, line := range strings.Split(text, "\n") {
		trimmed := strings.TrimLeft(line, " \t-")
		if !strings.HasPrefix(trimmed, "#") {
			continue
		}
		fn(i, line)
	}
}

var undefinedFunc = regexp.MustCompile(`function "([^"]+)" not defined`)

// goTemplate parses (and then executes) the file exactly as external-secrets'
// v2 engine does. Unknown function names are stubbed on demand rather than
// vendored: the engine ships sprig plus its own helpers, and this check is
// about syntax and executability, not about a function's return value.
func goTemplate(path, text string) []finding {
	funcs := template.FuncMap{}
	var tmpl *template.Template
	for attempt := 0; ; attempt++ {
		t, err := template.New(filepath.Base(path)).
			Funcs(funcs).
			Option("missingkey=error").
			Parse(text)
		if err == nil {
			tmpl = t
			break
		}
		if m := undefinedFunc.FindStringSubmatch(err.Error()); m != nil && attempt < 64 {
			funcs[m[1]] = func(_ ...any) any { return "" }
			continue
		}
		return []finding{{check: checkParse, file: path, line: templateErrLine(path, err),
			msg: fmt.Sprintf("does not parse as a Go text/template, so external-secrets "+
				"cannot render it and writes NO Secret at all: %v", err)}}
	}

	data := map[string]any{}
	for _, t := range tmpl.Templates() {
		if t.Tree != nil {
			collectFields(t.Tree.Root, data)
		}
	}
	if err := tmpl.Execute(discard{}, data); err != nil {
		return []finding{{check: checkExec, file: path, line: templateErrLine(path, err),
			msg: fmt.Sprintf("parses but does not execute: %v", err)}}
	}
	return nil
}

type discard struct{}

func (discard) Write(p []byte) (int, error) { return len(p), nil }

// templateErrLine digs the line number out of a text/template error, which
// formats as `template: <name>:<line>: <detail>`.
func templateErrLine(path string, err error) int {
	prefix := "template: " + filepath.Base(path) + ":"
	s := err.Error()
	i := strings.Index(s, prefix)
	if i < 0 {
		return 0
	}
	rest := s[i+len(prefix):]
	n := 0
	for _, r := range rest {
		if r < '0' || r > '9' {
			break
		}
		n = n*10 + int(r-'0')
	}
	return n
}

// collectFields walks the parse tree and seeds `data` with every top-level
// field the template dereferences, so Execute exercises the action tree
// instead of tripping over missingkey=error on the first reference. It cannot
// verify that a key really exists in the provider -- only external-secrets,
// holding the live secret, can do that.
func collectFields(n parse.Node, data map[string]any) {
	switch v := n.(type) {
	case nil:
		return
	case *parse.ListNode:
		if v == nil {
			return
		}
		for _, c := range v.Nodes {
			collectFields(c, data)
		}
	case *parse.ActionNode:
		collectFields(v.Pipe, data)
	case *parse.PipeNode:
		if v == nil {
			return
		}
		for _, c := range v.Cmds {
			collectFields(c, data)
		}
	case *parse.CommandNode:
		for _, a := range v.Args {
			collectFields(a, data)
		}
	case *parse.IfNode:
		collectFields(v.Pipe, data)
		collectFields(v.List, data)
		collectFields(v.ElseList, data)
	case *parse.RangeNode:
		collectFields(v.Pipe, data)
		collectFields(v.List, data)
		collectFields(v.ElseList, data)
	case *parse.WithNode:
		collectFields(v.Pipe, data)
		collectFields(v.List, data)
		collectFields(v.ElseList, data)
	case *parse.FieldNode:
		seed(data, v.Ident)
	case *parse.ChainNode:
		collectFields(v.Node, data)
		seed(data, v.Field)
	default:
		// Leaf nodes (text, literals, dot) reference nothing to seed.
	}
}

func seed(data map[string]any, idents []string) {
	cur := data
	for i, id := range idents {
		if i == len(idents)-1 {
			if _, ok := cur[id]; !ok {
				cur[id] = "x"
			}
			return
		}
		next, ok := cur[id].(map[string]any)
		if !ok {
			next = map[string]any{}
			cur[id] = next
		}
		cur = next
	}
}
