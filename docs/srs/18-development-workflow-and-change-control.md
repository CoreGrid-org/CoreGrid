# 18. Development Workflow and Change Control

This section sets out how changes to CoreGrid are proposed, reviewed and traced back to this specification. Day-to-day mechanics (setup, branch names, the pre-push checklist) are in `CONTRIBUTING.md`; this section covers the rules those mechanics enforce.

## 18.1 Branches and Pull Requests

- Work happens on feature branches cut from `development`, named for the component (`feature/<component>-<short-description>`). `main` only receives merges from `development`.
- Each feature gets one pull request, and every pull request is reviewed by at least one maintainer other than its author before merge. Self-merging is not allowed.
- CI (`.github/workflows/ci.yml`) must pass before merge. It runs a zero-warning backend build, the backend tests against a real PostgreSQL service container, and the frontend tests with a type-checked production build. The mobile repository's CI runs `flutter analyze` and `flutter test`.

## 18.2 Requirement Traceability

- Every requirement identifier (FR-, NFR-, AI-, DR-, INT-, OPS-) is cited in the issue that implements it and in the commit message or pull-request description. This lets a requirement such as FR-051 be traced to its code, its tests and its reviewer.
- A pull request links the GitHub issue it resolves, so delivery status is tracked in the issue tracker rather than in a hand-maintained file.

## 18.3 Shared Agent Contracts

Planner → Maintenance Analysis → Budget Analysis → Policy Compliance is a strict pipeline (§7.2): each agent's output is the next agent's input. The input/output contracts in §7.3 are therefore shared by all four component maintainers. Any change to them is a cross-component change (§12.2) and must be agreed by every affected maintainer before the agent's internal logic is changed.

## 18.4 Schema Changes

A change to `Domain/` or `CoreGridDbContext` comes with:
- an EF Core migration whose `Up()`/`Down()` contain only the intended change;
- the regenerated `backend/db/schema.sql`;
- a numbered export in `backend/db/migrations/`.

The physical reference design in Appendix E is updated in the same pull request whenever the migration diverges from it.

## 18.5 Scope Changes

This specification is the baseline contract for the implementation. Any change to scope, requirements or component ownership after baselining must:
1. be raised as a GitHub issue labelled `scope-change`;
2. be assessed against the delivery plan and the descope order in §15;
3. be approved by the maintainers;
4. be recorded in the revision history in the front matter before work begins.

## 18.6 AI-Assisted Development

AI coding assistants may be used. Every change must still follow one rule: the AI proposes, and the author reviews the diff, tests it and understands it before committing. Never generate and commit unread. Code that its author cannot explain, modify or debug is not merged. Secrets, credentials and private data are never shared with an AI tool.
