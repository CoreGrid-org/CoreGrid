# Cloudflare R2 (photo storage)

Maintenance and fault-report photos are stored privately in a Cloudflare R2 bucket. The backend uploads them (`POST /api/maintenance/photos`) and, on every read, hands the browser a signed link that expires after 15 minutes. The bucket is never public.

## 1. Create the bucket and a token

1. Cloudflare dashboard → **R2** → **Create bucket**, e.g. `coregrid-dev`. Leave public access **off**.
2. **R2** → **Manage R2 API Tokens** → **Create API token**:
   - Permissions: **Object Read & Write**
   - Specify bucket: the bucket above only
3. Copy the **Access Key ID** and **Secret Access Key** (shown once) and your **Account ID** (R2 overview page).

No CORS rules are needed: the browser never talks to R2 for uploads, only the backend does, and signed links are plain GETs.

## 2. Configure the backend

Credentials never go in `appsettings*.json`. Locally, put them in the git-ignored `backend/.env` (see `backend/.env.example`; the API loads it on start-up):

```dotenv
CloudflareR2__AccountId=<account id — the subdomain of the S3 API URL>
CloudflareR2__AccessKeyId=<S3 Access Key ID>
CloudflareR2__SecretAccessKey=<S3 Secret Access Key>
CloudflareR2__BucketName=<bucket name>
```

Only the **S3 credentials** (Access Key ID and Secret Access Key) are needed. The Cloudflare API token shown on the same page is not used by CoreGrid.

In Docker or the cloud, set the same four names as environment variables.

`CloudflareR2:PublicBaseUrl` is only needed for files uploaded as *public* objects; maintenance photos don't use it.

Restart the backend, then check `GET /health`: `photo-storage` should be `Healthy`. Until the credentials are set it reports `Degraded`, and uploads return a 503 "Photo storage isn't configured" message instead of failing silently.

## How it works

- Keys are `maintenance/<organisation id>/<random>-photo.<ext>`. The user's original file name is never used.
- A record can only reference a photo from its own organisation's folder; anything else is rejected with a 400 (`PhotoKeys.RequireOwnMaintenancePhoto`).
- Uploads are checked for size (≤ 5 MB), content type and real JPEG/PNG/WebP file signatures (`PhotoUploadValidator`).
- The AWS SDK for .NET v4 sends checksum headers R2 doesn't support by default; `CloudflareR2StorageService` sets `RequestChecksumCalculation`/`ResponseChecksumValidation` to `WHEN_REQUIRED` as Cloudflare recommends.
