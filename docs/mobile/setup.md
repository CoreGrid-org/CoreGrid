# CoreGrid Mobile — Setup and Operations

## Start the host services

From the main repository, start the backend, database, and ThunderID using the documented Docker Compose
workflow in [ThunderID setup](../setup/thunderid.md). Confirm the API and identity service are reachable over
the local HTTPS endpoints before starting Flutter.

## Android emulator and device networking

For an Android emulator, use the host loopback mapping supported by the local development setup. For a physical
device, use the computer's LAN address or `adb reverse` over USB as described by the mobile repository's
networking notes. The device and host must be able to reach both the API and ThunderID; a browser sign-in that
cannot redirect back to the app usually indicates a custom-scheme or client-registration problem.

## TLS

Trust local development certificates only in debug builds and only for local hosts. The Dio client and
`flutter_appauth` discovery/token requests must be able to validate the development certificate. The external
ThunderID browser flow remains subject to the browser's certificate rules. Production must use publicly trusted
certificates and standard validation.

## Run and verify

Provide the required runtime defines, then run:

```text
flutter pub get
dart run build_runner build --delete-conflicting-outputs
flutter analyze
flutter test
flutter run --dart-define-from-file=<development-config.json>
```

Do not commit config files containing client secrets or tokens. Refresh tokens belong in secure storage; API
base URLs and public client identifiers may be supplied as build-time configuration.

## ThunderID mobile client

Register a native/public mobile application with Authorization Code + PKCE, the app's custom redirect URI,
the `CoreGridUser` identity type, and the scopes needed for `/api/me`. Do not put a client secret in the app.
The development registration must be tested with a real sign-in, sign-out, refresh, unsupported-role gate, and
return-from-browser flow. Staging and production registrations are separate release prerequisites.

Password recovery requires a mobile application ID and recovery enabled in ThunderID. The app opens the hosted
recovery URL externally and never receives or stores the user's password.
