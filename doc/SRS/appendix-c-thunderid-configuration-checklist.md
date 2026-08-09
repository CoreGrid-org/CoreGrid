# Appendix C — ThunderID Configuration Checklist

| # | Configuration step | Verification |
|---|---|---|
| 1 | Create the root organisation for CoreGrid and record its issuer URL. | The OpenID configuration document resolves and lists the expected endpoints. |
| 2 | Record the organisation unit's identifier. Each department's deployment is self-hosted and single-tenant (Section 4.2), so no sub-organisation is created — there is only ever the one, default organisation unit. | The recorded identifier matches `ThunderID:OuId` in the backend configuration. |
| 3 | Register the React application as a single-page application with authorisation code and PKCE, and register the deployed and localhost redirect URIs. | Sign-in completes and no client secret is required. |
| 4 | Register the Flutter application as a mobile application with a custom-scheme redirect URI and PKCE. | Authentication opens in an external browser and returns to the application. |
| 5 | Register the CoreGrid API as a protected resource and define its scopes and audience identifier. | The `aud` claim in a test token contains the API identifier. |
| 6 | Define the four application roles (assigned instance-wide — there is no per-tenant scope to assign them within, Section 4.2). | The `roles` claim contains the expected values for a test user in each role. |
| 7 | Register a confidential machine-to-machine application for SCIM user provisioning and grant it user-management scope. | A test invitation creates a user in the deployment's single organisation unit. |
| 8 | Configure the token lifetimes: access token 15 minutes, refresh token rotation enabled. | A token's `exp` claim reflects the configured lifetime; a replayed refresh token is refused. |
| 9 | Enable multi-factor authentication for the Administrator role. | An administrator sign-in prompts for the second factor. |
| 10 | Record every identifier and secret in the deployment environment, never in the repository. | The CI secret scan passes and no identifier appears in tracked files. |
