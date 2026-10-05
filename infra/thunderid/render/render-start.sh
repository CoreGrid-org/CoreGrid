#!/bin/sh
set -e
# Render mounts Secret Files at /etc/secrets — copy them where ThunderID expects them.
cp /etc/secrets/deployment.yaml ./deployment.yaml
mkdir -p config/certs config/secrets
for f in server.cert server.key signing.cert signing.key ecdsa-signing.cert ecdsa-signing.key crypto.key; do
  cp "/etc/secrets/$f" "config/certs/$f"
done
cp /etc/secrets/direct_auth_secret config/secrets/direct_auth_secret
exec ./start.sh
