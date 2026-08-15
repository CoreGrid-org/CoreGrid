/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_THUNDERID_BASE_URL: string;
  readonly VITE_THUNDERID_CLIENT_ID: string;
  readonly VITE_THUNDERID_AFTER_SIGN_IN_URL: string;
  readonly VITE_THUNDERID_AFTER_SIGN_OUT_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

// Injected at build time from package.json — see vite.config.ts.
declare const __APP_VERSION__: string;
