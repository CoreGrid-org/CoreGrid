# CoreGrid developer commands. Run `make help` for the list.
#
# Local stack: ThunderID + PostgreSQL in Docker (docker-compose.yml), the API
# with `dotnet run` (http://localhost:5083) and the web app with Vite
# (http://localhost:5173). See README.md for first-time setup.

SHELL := /bin/bash
.DEFAULT_GOAL := help

BACKEND    := backend
TESTS      := backend.Tests
FRONTEND   := frontend
COMPOSE    := docker compose
THUNDERID_BUNDLE := oci://ghcr.io/thunder-id/thunderid-quick-start:latest

DB_CONNECTION ?= Host=localhost;Port=5433;Database=coregrid;Username=coregrid;Password=coregrid
IMAGE_TAG     ?= local

.PHONY: help
help: ## Show this help
	@awk 'BEGIN {FS = ":.*## "; printf "\nUsage: make \033[36m<target>\033[0m\n"} \
		/^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } \
		/^[a-zA-Z0-9_-]+:.*## / { printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2 }' $(MAKEFILE_LIST)
	@echo

##@ Setup

.PHONY: setup
setup: tools restore frontend-install env ## Install tools and dependencies, create frontend/.env
	@echo "Next: 'make infra-bootstrap' (first time) or 'make infra-up', then 'make db-update' and 'make dev'."

.PHONY: tools
tools: ## Install the EF Core CLI (dotnet-ef) if missing
	@command -v dotnet-ef >/dev/null 2>&1 || dotnet tool install --global dotnet-ef --version "10.*"

.PHONY: restore
restore: ## Restore backend and test NuGet packages
	dotnet restore $(BACKEND)
	dotnet restore $(TESTS)

.PHONY: frontend-install
frontend-install: ## Install frontend npm packages (clean, from the lockfile)
	cd $(FRONTEND) && npm ci

.PHONY: env
env: ## Create frontend/.env from .env.example (never overwrites)
	@if [ -f $(FRONTEND)/.env ]; then echo "frontend/.env already exists, leaving it alone."; \
	else cp $(FRONTEND)/.env.example $(FRONTEND)/.env && echo "Created frontend/.env; fill in VITE_THUNDERID_CLIENT_ID."; fi

.PHONY: secrets
secrets: ## List which backend user-secrets are set (values hidden)
	@cd $(BACKEND) && dotnet user-secrets list 2>/dev/null | sed -E 's/ = .*/ = <set>/' || true
	@echo "Set one with: cd backend && dotnet user-secrets set \"<Key>\" \"<value>\"  (see docs/setup/)"

##@ Infrastructure (Docker)

.PHONY: infra-bootstrap
infra-bootstrap: ## First run only: bootstrap ThunderID, then start everything
	$(COMPOSE) -f $(THUNDERID_BUNDLE) -p coregrid up -d
	$(COMPOSE) up -d

.PHONY: infra-up
infra-up: ## Start ThunderID and PostgreSQL
	$(COMPOSE) up -d

.PHONY: infra-stop
infra-stop: ## Stop the containers, keeping their data
	$(COMPOSE) stop

.PHONY: infra-down
infra-down: ## Remove the containers (volumes and data are kept)
	$(COMPOSE) down

.PHONY: infra-status
infra-status: ## Show container status
	$(COMPOSE) ps

.PHONY: infra-logs
infra-logs: ## Follow container logs
	$(COMPOSE) logs -f --tail=100

##@ Database

.PHONY: db-update
db-update: ## Apply EF Core migrations to the local database
	cd $(BACKEND) && dotnet ef database update --connection "$(DB_CONNECTION)"

.PHONY: db-migration
db-migration: ## Add a migration: make db-migration NAME=AddSomething
	@test -n "$(NAME)" || (echo "Usage: make db-migration NAME=DescriptiveName" && exit 1)
	cd $(BACKEND) && dotnet ef migrations add $(NAME)

.PHONY: db-schema
db-schema: ## Regenerate backend/db/schema.sql from the migrations
	cd $(BACKEND) && dotnet ef migrations script -o db/schema.sql

.PHONY: db-backup
db-backup: ## Dump the local database to backups/coregrid-<timestamp>.dump
	@mkdir -p backups
	docker exec coregrid-postgres pg_dump -U coregrid -d coregrid -Fc > backups/coregrid-$$(date +%Y%m%d-%H%M%S).dump
	@ls -1t backups | head -1

.PHONY: db-shell
db-shell: ## Open psql on the local database
	docker exec -it coregrid-postgres psql -U coregrid -d coregrid

##@ Run

.PHONY: dev
dev: ## Run the API and the web app together (Ctrl+C stops both)
	@$(MAKE) --no-print-directory -j2 backend frontend

.PHONY: backend
backend: ## Run the API on http://localhost:5083 (Swagger at /swagger)
	cd $(BACKEND) && dotnet run

.PHONY: backend-watch
backend-watch: ## Run the API with hot reload
	cd $(BACKEND) && dotnet watch run

.PHONY: frontend
frontend: ## Run the web app on http://localhost:5173
	cd $(FRONTEND) && npm run dev

.PHONY: health
health: ## Check the running API's health endpoint
	@curl -fsS http://localhost:5083/health && echo

##@ Quality

.PHONY: build
build: build-backend build-frontend ## Build everything

.PHONY: build-backend
build-backend: ## Build the API and tests with warnings as errors
	dotnet build $(BACKEND) -warnaserror
	dotnet build $(TESTS) -warnaserror

.PHONY: build-frontend
build-frontend: ## Type-check and build the web app
	cd $(FRONTEND) && npm run build

.PHONY: test
test: test-backend test-frontend ## Run all tests

.PHONY: test-backend
test-backend: ## Run backend tests (needs PostgreSQL for the append-only suite)
	TEST_DB_CONNECTION="$(DB_CONNECTION)" dotnet test $(TESTS)

.PHONY: test-frontend
test-frontend: ## Run frontend tests
	cd $(FRONTEND) && npm test

.PHONY: lint
lint: ## Lint the frontend
	cd $(FRONTEND) && npm run lint

.PHONY: check
check: build test ## Everything CI checks: build (zero warnings) and tests

##@ Docker images

.PHONY: docker-build
docker-build: docker-build-backend docker-build-frontend ## Build both production images

.PHONY: docker-build-backend
docker-build-backend: ## Build the API image (coregrid-api:local; override with IMAGE_TAG=)
	docker build -t coregrid-api:$(IMAGE_TAG) $(BACKEND)

.PHONY: docker-build-frontend
docker-build-frontend: ## Build the web image (coregrid-web:local); VITE_* URLs are build args
	docker build -t coregrid-web:$(IMAGE_TAG) $(FRONTEND)

##@ Housekeeping

.PHONY: clean
clean: ## Remove build output (bin/, obj/, dist/)
	rm -rf $(BACKEND)/bin $(BACKEND)/obj $(TESTS)/bin $(TESTS)/obj $(FRONTEND)/dist $(FRONTEND)/*.tsbuildinfo
