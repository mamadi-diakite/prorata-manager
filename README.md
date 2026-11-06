# prorata-manager

A web app to run the shared-cost account of a construction site, known in France as the "compte prorata".

On a building site, several contractors each work on their own lot. Some costs benefit everyone and cannot be charged to one lot: site water and power, cleaning of shared facilities, security, waste skips. These costs go into a shared account and are split between the contractors in proportion to their share of the work.

An account manager, either one of the contractors or a third party, runs the account: budget, expenses with invoices, calls for funds and the final balance. A control committee of contractor representatives approves unplanned or disputed expenses. Each contractor sees its share and its calls for funds.

## Who it is for

Account managers and contractors who run shared site costs today with spreadsheets and email threads.

## What it is not

- Not legal or accounting advice. The app applies the rules set in each site's agreement.
- No real payments and no link with accounting software.
- Demo organizations, sites, contractors and amounts are fictional.

## How the shared-cost account works

- **Site** (chantier): a construction project, with a project owner (the client who orders the works) and a project manager (the architect or engineer who supervises them).
- **Lot**: a part of the works given to one contractor. Each lot belongs to one of three families: structure, finishing works or building services.
- **Contractor** (entreprise titulaire): a company holding one or more lots of the site.
- **Contract amount** (montant du marché): the amount of a contractor's lots, excluding VAT. The contract amounts set each contractor's share of the work, so they are the basis of the split.
- **Account manager** (gestionnaire du compte prorata): the organization that runs the account. It can be one of the contractors, often the structural works contractor, or a third party such as a consulting firm.
- **Agreement** (convention de compte prorata): the rules the contractors sign for one site. A French standard describes default rules, and the contractors can agree on their own. The app applies the rules of each agreement and quotes no text from the standard.

The full glossary and the planned features are in [ROADMAP.md](ROADMAP.md).

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#)
- Database: SQLite with EF Core

## Status

Milestone M0 is done. What works today:

- Sign up a manager organization, saying whether it is a contractor or a third party, together with its first admin account. Log in, log out and change your password. Sessions expire after 60 minutes, and you are sent back to the login page when they do.
- Edit the organization's details (manager admins only).
- Create sites with their address, project owner, project manager, start date and planned handover date, list them, and move each site forward through preparation, in progress, handed over and closed.
- Add lots with a number, a name, a family and a contract amount in euros excluding VAT. The amount can wait until it is known.
- Add contractors with their company details and give them one or more lots. Each site shows its contract amounts in total, by family and by contractor, and the lots still missing an amount or a contractor.

The agreement, representatives, control committee, budget, expenses, split, calls for funds and closing are planned (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the API

The access tokens are signed with a key that is never stored in the repository. The API does not start without it. Set a base64 key of at least 32 bytes in the `Jwt__SigningKey` environment variable, and keep the same key between restarts so that issued tokens stay valid.

Bash:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
cd src/backend
dotnet run --project src/ProrataManager.Api --launch-profile http
```

PowerShell:

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:Jwt__SigningKey = [Convert]::ToBase64String($bytes)
cd src/backend
dotnet run --project src/ProrataManager.Api --launch-profile http
```

The API listens on http://localhost:5087. In Development it creates the SQLite database in `src/backend/src/ProrataManager.Api/App_Data/`, applies the migrations, and adds the demo data below when the database has no users.

### Run the web app

```bash
cd src/frontend
npm ci
npm start
```

Open http://localhost:4200. Calls to `/api` are sent to the API on port 5087.

### Demo data

All demo accounts use the password `TestDemo123!`. They are fictional and only exist in Development.

| Name | Email | Role | Organization | Account manager |
|------|-------|------|--------------|-----------------|
| Claire Martin | claire.martin@example.com | Manager admin | Atlas Structures | Contractor |
| Hugo Lambert | hugo.lambert@example.com | Manager admin | Prism Site Services | Third party |

| Site | Organization | Status | Lots | Contract amounts (excl. VAT) |
|------|--------------|--------|------|------------------------------|
| Linden Court housing, Lyon | Atlas Structures | In progress | 6 lots in the three families, 5 contractors; the painting lot has no contractor or amount yet | €3,401,450.95 |
| Harbour Point offices, Marseille | Atlas Structures | Preparation | 2 lots; the finishes lot has no contractor or amount yet | €2,310,000.00 |
| Maple School extension, Nantes | Prism Site Services | Handed over | 4 lots, 3 contractors | €1,410,616.05 |

To start again from an empty demo, stop the API and delete `src/backend/src/ProrataManager.Api/App_Data/`.

### Run the tests

Back end (with coverage, migrations and generated code excluded). The tests set their own signing key:

```bash
cd src/backend
dotnet test ProrataManager.slnx --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Project layout

```
src/backend/    ASP.NET Core solution: Domain, Application.Contracts, Application, Infrastructure, Api, and their test projects
src/frontend/   Angular workspace: projects/apps/prorata-manager-app and the shared libraries in projects/libs/shared
```

## License

[MIT](LICENSE)
