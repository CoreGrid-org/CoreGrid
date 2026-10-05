# ThunderID for a Render web service (demo hosting).
#
# Everything deployment-specific — deployment.yaml (Postgres on Neon, public_url,
# http_only) and the signing/encryption keys — comes from Render Secret Files,
# mounted at /etc/secrets at run time, so nothing secret is baked into the image.
FROM ghcr.io/thunder-id/thunderid:latest
COPY --chown=thunderid:thunderid render-start.sh /opt/thunderid/render-start.sh
CMD ["sh", "./render-start.sh"]
