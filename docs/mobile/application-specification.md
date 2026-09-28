# CoreGrid Mobile — Application Specification

## Architecture

The client is a Flutter application organised by capability:

```text
lib/
  app/                         bootstrap, ProviderScope, shell, and go_router
  features/<name>/             providers, API class, screens, and local widgets
  shared/api/                  Dio client, bearer interceptor, typed errors
  shared/auth/                 token storage, PKCE flow, auth state, guards
  shared/media/                compressed photo selection
  shared/widgets/              UI kit, status tones, async/error views, formatters
  shared/theme/                colors, spacing, radius, light/dark theme
```

Riverpod owns feature state through `AsyncNotifier`/`Notifier` providers. `go_router` owns navigation and
role guards. Feature API classes use one shared Dio client; screens do not make raw HTTP calls.

## Fixed technology choices

`flutter_riverpod`, `go_router`, `flutter_secure_storage`, `mobile_scanner`, `image_picker`, `dio`,
`flutter_appauth`, `flutter_image_compress`, `permission_handler`, `connectivity_plus`, `intl`,
`shared_preferences`, `freezed`, `json_serializable`, and `build_runner` are the documented package choices.
The app does not use a WebView for OAuth, a second state-management framework, or a local business-data
database. Offline persistence is a future enhancement; offline states must not present stale data as current.

## Navigation and roles

After sign-in, a role-filtered `StatefulShellRoute` provides these tabs:

- Officer: Home, Verify, Workflows, Faults, Account.
- Staff: Home, Faults, Account.
- Unresolved role: Home and Account until access is rejected.

Top-level full-screen routes include `/scan`, `/assets/...`, `/verification/:taskId`, `/campaigns/:id`,
`/workflows/new|:id`, `/maintenance/report|:id`, `/transfers`, and `/transfers/new|:id`.
Verification, workflows, campaigns, and transfers are Officer-only at the route level as well as in the UI.

## Authentication and API rules

Sign-in uses ThunderID Authorization Code + PKCE through an external user agent. Access tokens stay in memory;
refresh tokens use Android Keystore-backed secure storage. The interceptor attaches the bearer token, attempts
one silent refresh after a 401, retries once, and signs out after a second 401. Structured backend validation
errors become typed field errors. Token-bearing bodies are never logged.

The API contract uses snake_case JSON bodies and responses. Query parameters follow ASP.NET property names.
IDs are opaque strings. A selected asset must come from an authoritative API response, never from a display code.
Successful mutations invalidate affected providers and re-read server state.

## Screen flows

| Flow | Behaviour |
|---|---|
| Onboarding | Three pages shown once per install, stored as a non-secret local flag. |
| Authentication | Splash → ThunderID → token exchange → `/api/me` → Officer/Staff dashboard or access restricted. |
| Asset identification | Camera QR scan or manual code; unknown, refused-camera, offline, and cross-organisation cases recover safely. |
| Asset detail | Attribute-driven read view, lifecycle-aware actions, condition update, history, verify, and fault/transfer entry points. |
| Verification | Scan identifies the asset, then complete a pending task or begin an ad-hoc verification; discrepancies may include a compressed photo. |
| Maintenance | Report a fault with optional photo, track own reports, view Officer records, and start assigned work. |
| Transfers | Select an active asset, choose department then location, create request, and confirm receipt only after matching scan/code. |
| Workflows | Initiate an evaluation, poll status, and display recommendation/outcome; approval remains web-only. |
| Notifications | Read/unread inbox, unread badge, mark-read actions, and deep links to related records. |
| Password recovery | Open ThunderID's hosted recovery page externally; no password is processed by CoreGrid. |

## Configuration, build, and CI

Runtime values are supplied with `--dart-define-from-file` (or equivalent defines), including
`API_BASE_URL`, `THUNDERID_ISSUER`, `THUNDERID_CLIENT_ID`, and, when recovery is enabled,
`THUNDERID_APPLICATION_ID`. Debug builds may trust local development certificates only for local hosts;
production uses standard certificate validation.

The documented quality gate is `flutter analyze` followed by `flutter test`. CI should run generated-code
checks, analysis, unit/widget tests, and release build validation. Android release signing and staging/production
ThunderID registrations are deployment concerns and must not use development credentials.

## Security and AI-use policy

No embedded WebView OAuth, token logging, or mobile account registration is permitted. External AI may assist
development only when the owner reviews the diff, tests it, understands it, and records the tool, date, scope,
and verification. No external AI assistant is used during demonstration or viva.
