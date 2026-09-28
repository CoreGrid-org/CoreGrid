# CoreGrid Mobile Documentation

This section documents the Flutter mobile client maintained in the sibling `coregrid-mobile` repository. It records the mobile scope,
implementation status, ownership, setup requirements, feature contracts, and verification evidence alongside
the backend and web-client documentation in this repository.

## Documents

| Document | Purpose |
|---|---|
| [Application specification](application-specification.md) | Architecture, packages, routes, screen behaviour, configuration, CI, and testing strategy. |
| [Implementation progress](progress.md) | Requirement-by-requirement status, completed work, known gaps, and next milestone. |
| [Team allocation](team-allocation.md) | Feature ownership, requirement ranges, branch conventions, and review responsibilities. |
| [Setup and operations](setup.md) | Local networking, TLS, Flutter configuration, and ThunderID mobile-client setup. |
| [Asset detail feature](features/asset-detail.md) | Asset lookup, QR resolution, dynamic attributes, condition updates, and verification behaviour. |

The mobile repository also contains the original working notes under its `doc/` directory, including the full
mobile SRS and dated progress history. This `docs/mobile/` section is the main-repository copy of the current,
relevant documentation and should be updated with mobile implementation changes.

## Scope boundary

The mobile client is an online Flutter client for Inventory Officers and Department Staff. It uses the CoreGrid
API and ThunderID; it does not replace the React console. Administrator-only operations such as user
provisioning, approval, assignment, costing, disposal, and agent approval remain web-console responsibilities.

There is intentionally no mobile registration screen. Users are provisioned by an Administrator through the
React console and then sign in through ThunderID.

## Current snapshot

As of 2026-09-27, authentication, dashboards, asset scanning and search, asset detail, verification,
maintenance, transfers, workflows, notifications, and password-recovery entry points are implemented in the
mobile repository. `flutter analyze` reports zero issues and the latest recorded test run has 68 passing tests.
The remaining items and evidence limitations are listed in [Implementation progress](progress.md).
