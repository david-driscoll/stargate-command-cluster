# Work Routing

> **Inherited from the hub.** This is a spoke crew. Routing is owned by
> `home-operations-crew` and reached via the `upstream` entry in
> `.crew/upstream.json`. Do not maintain a second routing table here — it
> would drift from the hub every time the roster changes.
>
> **Canonical routing:** `home-operations/.crew/routing.md`
> (also fetched into `.crew/_upstream_repos/home-operations-crew/` by
> `crew upstream sync`, which runs from the post-merge / post-checkout hooks).

## Local overrides

None. If this repo ever needs a routing rule the hub does not have, add it
below and say why — anything not listed here follows the hub's table.

| Work Type | Route To | Why this repo differs |
|-----------|----------|-----------------------|
| _(none)_  |          |                       |

## Issue tracking

All issues for this repo are filed in `david-driscoll/vault`, labelled with
`repo:<this-repo>` so `crew-claude.yml` checks out the right tree.
