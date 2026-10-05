<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="frontend/public/assets/w-coregrid.webp">
    <img src="frontend/public/CoreGrid.png" alt="CoreGrid" width="140">
  </picture>
</p>

<h1 align="center">CoreGrid</h1>
<p align="center">Open-source asset lifecycle management, with AI help for repair, transfer and disposal decisions.</p>

CoreGrid helps an organisation register, track, maintain, transfer, verify and dispose of its physical assets. AI agents suggest what to do with an asset, and a person approves anything important. Each organisation runs its own copy.

## Live demo

Try it at **https://demo-coregrid.vercel.app**. The API reference is live at https://coregrid-v7jn.onrender.com/swagger. The password for every account is `Login@123456`.

| Account | Role |
|---|---|
| `admin@coregrid.test` | Administrator |
| `officer@coregrid.test` | Inventory Officer (web and mobile) |
| `auditor@coregrid.test` | Auditor |
| `staff@coregrid.test` | Department Staff (mobile app only) |

The demo runs on free hosting that sleeps when idle, so the first sign-in can take about a minute. See [Deployment](docs/setup/deployment.md) for how it is hosted.

## Quick start

You need: .NET 10 SDK, Node 20+, Docker, `make`, `curl` and `jq`.

```bash
./setup.sh    # one-time setup
make dev      # start the API and the web app
```

Then open:

- Web app: http://localhost:5173
- API docs (Swagger): http://localhost:5083/swagger

Test logins: `admin@coregrid.test`, `officer@coregrid.test`, `auditor@coregrid.test`, `staff@coregrid.test`. The password for all of them is `Login@123456` (local only).

Next time, just run `make infra-up && make dev`.

## Setup guides

| Guide | What it covers |
|---|---|
| [Sign-in (ThunderID)](docs/setup/thunderid.md) | One-time sign-in setup. Do this first on a new machine |
| [AI agents](docs/setup/ai-agents.md) | Adding a Gemini key (optional; works without one) |
| [Photo storage (Cloudflare R2)](docs/setup/cloudflare-r2.md) | Storing fault and verification photos |
| [Full developer setup](CONTRIBUTING.md) | Manual steps, project structure and how to contribute |
| [Backend settings](backend/.env.example) | Every backend setting, with examples |
| [Web app](frontend/README.md) | Web app settings and commands |
| [Scripts](scripts/README.md) | Helper scripts and performance tests |
| [Deployment](docs/setup/deployment.md) | The live demo on Vercel, Render and Neon, and how to reproduce it |

## Architecture

![CoreGrid integrated architecture](docs/diagrams/integrated-architecture.png)

The web and mobile apps sign in through ThunderID and call one ASP.NET Core API. The API runs the AI agents itself and is the only part that talks to PostgreSQL, photo storage and the AI provider. More diagrams are in [System Architecture](docs/srs/03-system-architecture.md).

## What's in this repository

| Folder | Contents |
|---|---|
| `backend/` | API (C# / ASP.NET Core) and database migrations |
| `backend.Tests/` | Backend tests |
| `frontend/` | Web app (React) |
| `docs/` | Requirements, architecture decisions and setup guides |
| `scripts/` | Helper scripts |

Related repositories:

- [coregrid-mobile](https://github.com/CoreGrid-org/coregrid-mobile): Android app for field work
- [coregrid-web](https://github.com/CoreGrid-org/coregrid-web): public website and user manual

## Running tests

```bash
make check    # build and run all tests (same as CI)
```

## Learn more

- [Requirements (SRS)](docs/srs/00-front-matter.md)
- [How the AI agents work](docs/srs/07-agentic-ai-subsystem-requirements.md)
- [Architecture decisions](docs/architecture/decision-records.md)
- [Issue tracker](https://github.com/CoreGrid-org/CoreGrid/issues)

## License

Apache License 2.0. See [LICENSE](LICENSE).
