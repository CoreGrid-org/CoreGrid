# CoreGrid web app: production image (static build served by nginx).
#   docker build -t coregrid-web \
#     --build-arg VITE_API_URL=https://api.example.org/api \
#     --build-arg VITE_THUNDERID_BASE_URL=https://id.example.org \
#     --build-arg VITE_THUNDERID_CLIENT_ID=<client id> \
#     --build-arg VITE_THUNDERID_AFTER_SIGN_IN_URL=https://app.example.org \
#     --build-arg VITE_THUNDERID_AFTER_SIGN_OUT_URL=https://app.example.org \
#     frontend/
#
# Vite inlines VITE_* values at build time, so they are build arguments, not
# runtime environment variables. The defaults below match local development.

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG VITE_API_URL=http://localhost:5083/api
ARG VITE_THUNDERID_BASE_URL=https://localhost:8090
ARG VITE_THUNDERID_CLIENT_ID=
ARG VITE_THUNDERID_AFTER_SIGN_IN_URL=http://localhost:5173
ARG VITE_THUNDERID_AFTER_SIGN_OUT_URL=http://localhost:5173
ENV VITE_API_URL=$VITE_API_URL \
    VITE_THUNDERID_BASE_URL=$VITE_THUNDERID_BASE_URL \
    VITE_THUNDERID_CLIENT_ID=$VITE_THUNDERID_CLIENT_ID \
    VITE_THUNDERID_AFTER_SIGN_IN_URL=$VITE_THUNDERID_AFTER_SIGN_IN_URL \
    VITE_THUNDERID_AFTER_SIGN_OUT_URL=$VITE_THUNDERID_AFTER_SIGN_OUT_URL

RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
