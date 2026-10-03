package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// The fixture reproduces the real shape: a kustomization whose
// configMapGenerator slurps values.yaml, and an ExternalSecret that renders
// that ConfigMap key with `templateAs: Values`.
const fixtureKustomization = `apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ./externalsecret.yaml
configMapGenerator:
  - name: ${APP}-values
    files:
      - values.yaml=./values.yaml
`

const fixtureExternalSecret = `apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: ${APP}-values
spec:
  target:
    template:
      templateFrom:
      - target: Data
        configMap:
          name: ${APP}-values
          items:
          - key: values.yaml
            templateAs: Values
`

// writeFixture lays down a one-app tree and returns its root.
func writeFixture(t *testing.T, values, kustomization string) string {
	t.Helper()
	root := t.TempDir()
	dir := filepath.Join(root, "app")
	if err := os.MkdirAll(dir, 0o750); err != nil {
		t.Fatal(err)
	}
	files := map[string]string{
		"kustomization.yaml":  kustomization,
		"externalsecret.yaml": fixtureExternalSecret,
		"values.yaml":         values,
	}
	for name, body := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(body), 0o600); err != nil {
			t.Fatal(err)
		}
	}
	return root
}

func checksFor(t *testing.T, values string) []string {
	t.Helper()
	rep, err := scan([]string{writeFixture(t, values, fixtureKustomization)})
	if err != nil {
		t.Fatalf("scan: %v", err)
	}
	if rep.checkedGenerated != 1 || rep.checkedTemplates != 1 {
		t.Fatalf("discovery failed: generated=%d templated=%d (want 1/1)",
			rep.checkedGenerated, rep.checkedTemplates)
	}
	var got []string
	for _, f := range rep.findings {
		got = append(got, f.check)
	}
	return got
}

func TestChecks(t *testing.T) {
	for _, tc := range []struct {
		name  string
		body  string
		want  []string
		never []string
	}{
		{
			// The actual outage: vault#111 / equestria-cluster#2980.
			name: "unparseable action in a comment fails like external-secrets does",
			body: "---\n# see {{ ... }} for the syntax\nkey: value\n",
			want: []string{"ESO-COMMENT", "ESO-PARSE"},
		},
		{
			// #2977: parses fine, but envsubst ate the comment.
			name: "live substitution in a comment",
			body: "---\n# set this from ${SOME_VAR}\nkey: value\n",
			want: []string{"ENVSUBST"},
		},
		{
			// Regression guard: `$$` is the escape and must stay silent, or
			// every values file carrying $${REGISTRATION_TOKEN} goes red.
			name:  "escaped substitution in a comment is not a finding",
			body:  "---\n# literal $${REGISTRATION_TOKEN} reaches the container\nkey: value\n",
			never: []string{"ENVSUBST"},
		},
		{
			name: "parseable action in a comment still leaks a secret into it",
			body: "---\n# example: {{ .password }}\nkey: \"{{ .password }}\"\n",
			want: []string{"ESO-COMMENT"},
		},
		{
			name:  "ordinary templated values file is clean",
			body:  "---\n# a perfectly ordinary comment\nkey: \"{{ .password }}\"\n",
			never: []string{"ESO-COMMENT", "ESO-PARSE", "ESO-EXEC", "ENVSUBST"},
		},
	} {
		t.Run(tc.name, func(t *testing.T) {
			got := checksFor(t, tc.body)
			for _, w := range tc.want {
				if !contains(got, w) {
					t.Errorf("missing %s; got %v", w, got)
				}
			}
			for _, n := range tc.never {
				if contains(got, n) {
					t.Errorf("unexpected %s; got %v", n, got)
				}
			}
		})
	}
}

// A file that is generated but NOT rendered by external-secrets must still be
// checked for envsubst, and must never be reported as a broken template.
func TestUntemplatedFileOnlyGetsEnvsubstCheck(t *testing.T) {
	root := writeFixture(t, "---\n# mentions {{ .password }} and ${SOME_VAR}\nkey: value\n",
		strings.Replace(fixtureKustomization, "${APP}-values", "other-values", 1))
	rep, err := scan([]string{root})
	if err != nil {
		t.Fatalf("scan: %v", err)
	}
	if rep.checkedTemplates != 0 {
		t.Fatalf("file should not be treated as ESO-templated, got %d", rep.checkedTemplates)
	}
	for _, f := range rep.findings {
		if f.check != "ENVSUBST" {
			t.Errorf("unexpected %s on an untemplated file", f.check)
		}
	}
}

// The reported line number has to be usable, since that is how the error gets
// found: the live cluster pointed at values.yaml:8.
func TestParseErrorReportsLineNumber(t *testing.T) {
	body := "---\n#\n#\n#\n#\n#\n#\n# {{ ... }}\nkey: value\n"
	rep, err := scan([]string{writeFixture(t, body, fixtureKustomization)})
	if err != nil {
		t.Fatalf("scan: %v", err)
	}
	for _, f := range rep.findings {
		if f.check == "ESO-PARSE" {
			if f.line != 8 {
				t.Errorf("ESO-PARSE line = %d, want 8", f.line)
			}
			return
		}
	}
	t.Fatal("no ESO-PARSE finding")
}

func TestUnknownFunctionsDoNotFailTheParse(t *testing.T) {
	// external-secrets ships sprig; this tool stubs names on demand rather
	// than vendoring it, so a sprig pipeline must not be reported as broken.
	got := checksFor(t, "---\nkey: \"{{ .password | b64enc | quote }}\"\n")
	for _, c := range got {
		if c == "ESO-PARSE" || c == "ESO-EXEC" {
			t.Errorf("sprig pipeline wrongly reported as %s", c)
		}
	}
}

func contains(hay []string, needle string) bool {
	for _, h := range hay {
		if h == needle {
			return true
		}
	}
	return false
}
