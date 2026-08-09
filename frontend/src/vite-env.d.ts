/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_THUNDERID_BASE_URL: string;
  readonly VITE_THUNDERID_CLIENT_ID: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
