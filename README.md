# oMalayisha (Malayisha)

A trust-first marketplace connecting senders in South Africa with **oMalayisha** (cross-border goods transporters) travelling to Zimbabwe to deliver goods.

## Monorepo structure

```
malayisha/
├── src/
│   ├── Malayisha.Api/              # ASP.NET Core 10 Web API
│   ├── Malayisha.Application/      # Use cases, MediatR handlers, DTOs, validators
│   ├── Malayisha.Domain/           # Entities, enums, domain rules
│   └── Malayisha.Infrastructure/   # EF Core, Redis, S3, SMS, SignalR
├── tests/
│   ├── Malayisha.Application.Tests/
│   └── Malayisha.Api.IntegrationTests/
├── apps/
│   ├── mobile/                     # React Native (Expo SDK 57)
│   ├── web/                        # Next.js 16 marketing + PWA
│   └── admin/                      # Next.js 16 admin dashboard
├── infra/
│   └── terraform/
└── docs/
```

## Tech stack

| Layer | Technology |
|-------|------------|
| API | ASP.NET Core 10 (C#), Clean Architecture |
| Mobile | React Native 0.86 via Expo SDK 57 |
| Web / Admin | Next.js 16, React 19, TypeScript, Tailwind CSS |
| Database | PostgreSQL 16 (planned) |

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- PostgreSQL 16 (for later tasks)

## Getting started

### API

```bash
dotnet restore
dotnet build
dotnet run --project src/Malayisha.Api
```

Health check: `GET http://localhost:5xxx/api/health`

### Tests

```bash
dotnet test
```

### Mobile (Expo)

```bash
cd apps/mobile
npm install
npm start
```

### Web

```bash
cd apps/web
npm install
npm run dev
```

Runs at [http://localhost:3000](http://localhost:3000).

### Admin

```bash
cd apps/admin
npm install
npm run dev
```

Runs at [http://localhost:3001](http://localhost:3001).

## Status

Pre-MVP — solution scaffold in place; domain and feature implementation in progress.

See [docs/PRODUCT_PLAN.md](docs/PRODUCT_PLAN.md) for the full product plan.
