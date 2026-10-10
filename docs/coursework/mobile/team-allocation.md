# CoreGrid Mobile — Team Allocation

| Owner | Mobile scope | Requirements |
|---|---|---|
| Jayashan Guruge | `features/scan/`, `features/assets/` | FR-016–FR-032 |
| Seneja Ramanayaka | `features/maintenance/`, `features/notifications/` | FR-033–FR-042, FR-077–FR-080 |
| Nipuna Bhanuka Samarasinghe | `features/transfers/` | FR-043–FR-055 |
| Hasitha Erandika | `features/verification/`, `features/workflows/`, app shell, authentication, dashboard | FR-056–FR-069, FR-076, plus cross-cutting shell |

The allocation follows the main SRS component ownership. The app shell, sign-in, dashboard, and shared client
are cross-cutting responsibilities and are documented as Hasitha's coordinating scope. No owner has a
registration screen because account provisioning is an Administrator-only React responsibility.

The supplied PR history confirms the practical split: `jguruge` leads Asset/QR work, `seneja` leads
maintenance/notifications, `NipunaBhanuka18` leads transfer/disposal and the Budget Agent, and
`HasithaErandika` leads authentication, app shell, verification/workflows, CI, and consolidated documentation.
FR-049 is assigned to Component C but is not currently verified in the checked mobile tree.

Each contribution should reference its requirement IDs, use a feature branch, receive review from another
member, and pass `flutter analyze` and `flutter test`. Owners must review and be able to explain their final
implementation, including work initially assisted by AI tools.
