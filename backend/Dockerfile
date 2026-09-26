# CoreGrid API: production image.
#   docker build -t coregrid-api backend/        (or: make docker-build-backend)
#
# Configuration comes from environment variables at run time; nothing secret
# is baked in. The ones you'll need (double underscore = nested key):
#   ConnectionStrings__CoreGrid, Cors__AllowedOrigins__0,
#   ThunderID__Issuer, ThunderID__Resource, ThunderID__OuId, ThunderID__ScimClientId,
#   ThunderID__ScimClientSecret, ThunderID__RoleIds__Administrator (etc.),
#   Llm__ApiKey, CloudflareR2__AccountId/AccessKeyId/SecretAccessKey/BucketName
# See docs/setup/ for what each one is.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so code changes don't re-download packages.
COPY CoreGrid.Api.csproj ./
RUN dotnet restore CoreGrid.Api.csproj

COPY . .
RUN dotnet publish CoreGrid.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# QuestPDF (report exports) renders text through fontconfig.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app .

# The base image ships a non-root "app" user; don't run the API as root.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoreGrid.Api.dll"]
