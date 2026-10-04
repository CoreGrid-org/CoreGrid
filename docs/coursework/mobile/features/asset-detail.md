# Mobile Asset Detail and Identification

## Routes

```text
/assets          manual asset-code lookup
/scan            camera QR scan
/assets/:id      authoritative asset detail
/assets/:id/verify  Officer ad-hoc physical verification
```

The dashboard's Find an asset card first attempts an exact code. Other input opens the search view with the
query prefilled. QR values resolve through `GET /api/assets/qr/{code}` and open the returned record.

## API contract

| Operation | Endpoint |
|---|---|
| Load detail | `GET /api/assets/{id}` |
| Resolve code | `GET /api/assets/qr/{code}` |
| Search | `GET /api/assets` |
| Update condition | `PATCH /api/assets/{id}/condition` with `{ "condition": "GOOD" }` |
| History | `GET /api/assets/{id}/history?page=&page_size=` |
| Ad-hoc verify | `POST /api/assets/{id}/verify` |

Accepted conditions are `NEW`, `GOOD`, `FAIR`, `POOR`, and `UNSERVICEABLE`. A successful condition update is
followed by provider invalidation so the screen reads the server's value and history entry.

## Behaviour

The detail view renders attributes by their backend `data_type`, not by attribute name. `TEXT`, `NUMBER`,
`DATE`, `BOOLEAN`, and `SELECT` values are supported, with a scalar fallback for future types. Condition update
is offered only for supported active lifecycles and to roles allowed by the API. Staff never see Verify, and
the backend remains the final authorization authority.

Unknown or cross-organisation codes show a non-leaking “Asset not found” state. Offline lookup shows an offline
state and does not present stale cached business data. Camera refusal always leaves manual code entry available.

## Tests

The mobile repository records tests for condition parsing, attribute-driven rendering, role visibility,
lifecycle gating, not-found handling, offline handling, and manual lookup validation. Device-level camera and
live-backend testing remains part of release verification.
