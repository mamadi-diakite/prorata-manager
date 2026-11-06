# Roadmap

This is the working plan for prorata-manager. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- Sites with lots, contractors and contract amounts, run by an account manager that is a contractor or a third party.
- The site agreement, representatives, control committee, budget, expenses with invoices and committee votes.
- Progress statements, the split engine, calls for funds, payments, the final statement, clearances and meetings.

Out of scope:
- Not legal or accounting advice. The app applies the rules set in each site's agreement. It quotes no text from the standard.
- No real payments and no bank link. Payments are recorded by hand.
- No link with accounting software and no legal e-invoicing.
- Not a full site management tool: no planning, no daily reports, no works progress tracking beyond the amounts needed for the split.
- No real company data. The demo runs on fictional sites, contractors and amounts.

## How the shared-cost account works

On a building site, several contractors work side by side, each on its own lot. Some costs benefit everyone and cannot be charged to one lot: site water and power, cleaning of the shared site office and toilets, security guards, waste skips. These costs go into a shared account and are split between the contractors in proportion to their share of the work. The French standard NF P03-001 describes the default rules; contractors can agree on their own rules in a signed agreement.

An account manager runs the account for all contractors. It can be one of the contractors, often the main structural works contractor, or a third party such as a consulting firm. The manager sets the budget, records expenses with their invoices, issues calls for funds, and settles the final balance. A control committee of contractor representatives approves unplanned or disputed expenses and the final balance.

## Glossary

| Term | French term | Meaning |
|------|-------------|---------|
| Site | Chantier | A construction project with its lots and contractors |
| Project owner | Maître d'ouvrage | The client who orders the works. Stored for information |
| Project manager | Maître d'œuvre | The architect or engineer who supervises the works and reports progress amounts |
| Lot | Lot | A part of the works given to one contractor, in one of three families: structure, finishing works, building services |
| Contractor | Entreprise titulaire | A company holding one or more lots |
| Contract amount | Montant du marché | The amount of a contractor's lots, plus amendments |
| Shared-cost account | Compte prorata | The account where shared site costs are recorded and split |
| Agreement | Convention de compte prorata | The rules signed by the contractors for one site |
| Account manager | Gestionnaire du compte prorata | The organization that runs the account: a contractor or a third party |
| Control committee | Comité de contrôle | An odd number of contractor representatives who approve unplanned or disputed expenses and the final balance |
| Representative | Représentant | The person who acts for a contractor on the site |
| Budget | Budget prévisionnel | The planned shared costs, line by line |
| Expense | Dépense | A shared cost backed by an invoice |
| Restricted expense | Dépense à répartition limitée | An expense split only between some named contractors |
| Progress statement | Situation de travaux | The cumulative amount of work done by a contractor at a date |
| Allocation key | Clé de répartition | What the split is based on: cumulative progress statements or contract amounts |
| Management fee | Rémunération du gestionnaire | A percentage of the expenses, paid to the account manager |
| Actual-cost mode | Gestion au réel | Contractors pay their share of the actual expenses |
| Flat-rate mode | Gestion au forfait | Contractors pay a fixed percentage of their progress statements |
| Call for funds | Appel de fonds | A request to each contractor to pay its share for a period |
| Final statement | Décompte définitif | The final balance of each contractor after the works are handed over |
| Clearance | Quitus | The confirmation that a contractor has met all its obligations to the account |

## Roles

| Role | Can do |
|------|--------|
| Manager admin | Manage the manager organization and its staff, create sites |
| Manager staff | Run the account of the sites they are assigned to |
| Representative | See its contractor's data on a site, submit expenses it paid, approve or dispute its calls for funds |
| Committee member | A representative who also votes on expenses and on the final balance |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable app: accounts, manager organization, sites with lots, contractors and contract amounts |
| M1 Agreement and people | Agreement settings, representatives, control committee, budget |
| M2 Expenses | Expenses with invoices, advanced by contractors, committee votes, disputes, restricted expenses |
| M3 Calls for funds | Progress statements, split, calls for funds, payments, contractor dashboard, flat-rate mode |
| M4 Closing and meetings | Final statement, observations, clearance, meetings and minutes, audit trail |
| M5 Quality | CI, tests, accessibility, docs |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: Api, Domain, Application, Infrastructure and Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, first migration, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the main app and shared libraries | M0 |
| SETUP-5 | GitHub Actions: build and test both API and front end on each push | M5 |

## EPIC 2 — Accounts and manager organization

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create a manager organization and its first admin account | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password | M0 |
| AUTH-5 | Demo organizations, sites and accounts are seeded with a documented demo password | M0 |
| ORG-1 | As a manager admin, I edit my organization's details and say whether it is a contractor or a third party | M0 |
| ORG-2 | As a manager admin, I invite staff members and assign them to sites | M1 |

Acceptance notes:
- Passwords are hashed. Demo emails use `@example.com`.

## EPIC 3 — Sites, lots and contractors

| ID | Story | Milestone |
|----|-------|-----------|
| SITE-1 | As a manager, I create a site with name, address, project owner, project manager, start date and planned handover date | M0 |
| SITE-2 | As a manager, I list my sites with their status: preparation, in progress, handed over, closed | M0 |
| SITE-3 | As a manager, I add lots with number, name and family (structure, finishing works, building services) | M0 |
| SITE-4 | As a manager, I add contractors with company details and assign them one or more lots | M0 |
| SITE-5 | As a manager, I record each lot's contract amount excluding VAT | M0 |
| SITE-6 | As a manager, I add amendments to a lot's contract amount with a date and a reason | M1 |

Acceptance notes:
- Contract amounts are positive, in euros with two decimals.
- A contractor cannot be removed once it has an expense, a progress statement or a call for funds.

## EPIC 4 — Agreement

| ID | Story | Milestone |
|----|-------|-----------|
| AGREE-1 | As a manager, I set the site's agreement: actual-cost or flat-rate mode, allocation key, management fee rate | M1 |
| AGREE-2 | Defaults follow the usual rules: actual-cost mode, split on cumulative progress statements, 8% management fee | M1 |
| AGREE-3 | As a manager, I set the expense deadline: invoices submitted later than N days after the work are refused | M1 |
| AGREE-4 | As a manager, I set the call for funds frequency (monthly or quarterly) and the payment term | M1 |
| AGREE-5 | In flat-rate mode, I set the rate applied to each progress statement | M1 |
| AGREE-6 | As a representative, I see the agreement of my site | M1 |
| AGREE-7 | Agreement changes after the first call for funds need a committee vote and are kept in a history | M2 |

## EPIC 5 — Representatives and committee

| ID | Story | Milestone |
|----|-------|-----------|
| PEOPLE-1 | As a manager, I invite one representative per contractor by email | M1 |
| PEOPLE-2 | As an invited representative, I accept the invitation and create my account | M1 |
| PEOPLE-3 | As a manager, I appoint the control committee: an odd number of representatives covering the three lot families | M1 |
| PEOPLE-4 | The committee cannot be saved if the number is even or a family is missing; the screen says what is wrong | M1 |
| PEOPLE-5 | As a manager, I replace a representative or a committee member | M1 |

## EPIC 6 — Budget

| ID | Story | Milestone |
|----|-------|-----------|
| BUDGET-1 | As a manager, I build the budget from a catalog of usual shared costs: water, power, site office cleaning, toilets cleaning, security guards, waste skips, heating | M1 |
| BUDGET-2 | As a manager, I add custom budget lines | M1 |
| BUDGET-3 | As a manager, I mark a line as restricted to named contractors, with its own allocation key | M1 |
| BUDGET-4 | As a manager or representative, I see the budget against actual expenses, line by line | M2 |

## EPIC 7 — Expenses

| ID | Story | Milestone |
|----|-------|-----------|
| EXP-1 | As a manager, I record an expense on a budget line with supplier, date, amounts excluding VAT and including VAT, and the invoice file | M2 |
| EXP-2 | As a representative, I submit an expense my company paid for everyone, with its invoice | M2 |
| EXP-3 | Expenses submitted after the agreement's deadline are refused with the reason | M2 |
| EXP-4 | Expenses outside the budget need a committee vote before they count | M2 |
| EXP-5 | As a committee member, I vote for or against a pending expense. Majority decides; the result is recorded | M2 |
| EXP-6 | As a representative, I dispute a counted expense with a reason. The committee decides | M2 |
| EXP-7 | As anyone on the site, I see the list of expenses with their status and open each invoice | M2 |
| EXP-8 | Invoices are PDF or images, checked by content, stored outside the web root | M2 |

## EPIC 8 — Progress statements and split

| ID | Story | Milestone |
|----|-------|-----------|
| SPLIT-1 | As a manager, I record each contractor's cumulative progress statement for a period, as reported by the project manager | M3 |
| SPLIT-2 | A cumulative amount lower than the previous one is refused unless I confirm a correction | M3 |
| SPLIT-3 | The split engine computes each contractor's share from the allocation key; restricted expenses use their own contractors and key | M3 |
| SPLIT-4 | Shares are rounded to the cent and always add up exactly to the total (largest remainder method) | M3 |
| SPLIT-5 | As a manager or representative, I see the split at any date with the formula and the inputs used | M3 |

Acceptance notes:
- The split engine is a pure function covered by unit tests, including zero progress, a contractor joining late, restricted expenses and rounding.

## EPIC 9 — Calls for funds and payments

| ID | Story | Milestone |
|----|-------|-----------|
| CALL-1 | As a manager, I issue a call for funds for a period: each contractor's share of counted expenses plus the fee, minus what it already paid | M3 |
| CALL-2 | In flat-rate mode, the call is the agreed rate applied to the contractor's progress since the last call | M3 |
| CALL-3 | As a representative, I see my calls for funds with the detail, and approve or dispute each one | M3 |
| CALL-4 | As a manager, I record payments, full or partial, with date and reference | M3 |
| CALL-5 | As a manager, I see overdue calls and send a reminder notification | M3 |
| CALL-6 | As a representative, I see my dashboard: share, total called, paid, still due | M3 |
| CALL-7 | Calls for funds and payments are independent from what the project owner pays the contractor | M3 |

## EPIC 10 — Closing

| ID | Story | Milestone |
|----|-------|-----------|
| CLOSE-1 | As a manager, I record the handover date and the final contract amounts | M4 |
| CLOSE-2 | As a manager, I compute the final statement: each contractor's final share, what it paid, the balance to pay or to refund | M4 |
| CLOSE-3 | As a representative, I send observations on the final statement within the agreed period | M4 |
| CLOSE-4 | As a committee member, I vote to settle the final statement after the observations | M4 |
| CLOSE-5 | As a manager, I issue a clearance to each contractor whose balance is settled | M4 |
| CLOSE-6 | As a manager, I export the final statement and the clearances as PDF | M4 |

## EPIC 11 — Meetings

| ID | Story | Milestone |
|----|-------|-----------|
| MEET-1 | As a manager, I schedule a shared-cost account meeting with an agenda; representatives are notified | M4 |
| MEET-2 | As a manager, I record who attended, the decisions and the minutes | M4 |
| MEET-3 | As a representative, I see past meetings and their minutes | M4 |

## EPIC 12 — Notifications and audit trail

| ID | Story | Milestone |
|----|-------|-----------|
| NOTIF-1 | Notifications go through a sender interface. The first implementation stores them in-app | M2 |
| NOTIF-2 | As a user, I see my notifications with an unread count | M2 |
| AUDIT-1 | Every change to the agreement, expenses, votes, calls, payments and the final statement is recorded with date and author | M4 |
| AUDIT-2 | As a manager or committee member, I see the audit trail of a site | M4 |

## EPIC 13 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | API integration tests for the full cycle: budget, expenses, votes, calls, payments, final statement | M5 |
| QA-2 | Unit tests for the split engine with worked examples documented in the README | M5 |
| QA-3 | End-to-end test: manager records an expense, committee approves, call issued, representative approves (Playwright) | M5 |
| QA-4 | Accessibility pass on the representative pages | M5 |
| QA-5 | Docs: how the shared-cost account works, the split formulas, demo accounts | M5 |

---

## Later ideas

- Email delivery of notifications and calls for funds
- Import of progress statements from a spreadsheet
- Several manager organizations sharing a site
- Excel export of the account
- Read-only access for the project manager
- French and English UI (the domain is French, so a French UI comes first among the later ideas)
