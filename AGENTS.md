# AGENTS.md — instruction file (generato da `oa`)

> File canonico letto dagli agenti AI (Claude, GitHub Copilot, …). Gli altri (CLAUDE.md, `.github/copilot-instructions.md`) sono symlink a questo.
> **Non modificare a mano**: è rigenerato da `oa`.
> Ogni voce ha il link Markdown (leggibile) e il riferimento `@percorso`, che i tool usano per **includere** automaticamente il file.

Stack: **backend_net** · modalità **link**

## Ordine di lettura

- [BASE_INSTRUCTION.md](.oa/core/BASE_INSTRUCTION.md) · @.oa/core/BASE_INSTRUCTION.md
- [INSTRUCTIONS_backend_net.md](.oa/stacks/backend_net/INSTRUCTIONS_backend_net.md) · @.oa/stacks/backend_net/INSTRUCTIONS_backend_net.md
- [ARCHITECTURE.md](.oa/stacks/backend_net/ARCHITECTURE.md) · @.oa/stacks/backend_net/ARCHITECTURE.md
- [DATABASE.md](.oa/stacks/backend_net/DATABASE.md) · @.oa/stacks/backend_net/DATABASE.md
- [SECURITY.md](.oa/stacks/backend_net/SECURITY.md) · @.oa/stacks/backend_net/SECURITY.md
- [TESTING.md](.oa/stacks/backend_net/TESTING.md) · @.oa/stacks/backend_net/TESTING.md

Gerarchia in caso di conflitto: **Core > Stack > Personalizzazione di progetto > DEVELOPER.md**.

## Agent skills

GitHub issues tracked with `gh` CLI. See `docs/agents/issue-tracker.md`.

Triage labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

Domain docs: single-context layout (CONTEXT.md + docs/adr/). See `docs/agents/domain.md`.
