# prorata-manager

A web app to run the shared-cost account of a construction site, known in France as the "compte prorata".

On a building site, several contractors each work on their own lot. Some costs benefit everyone and cannot be charged to one lot: site water and power, cleaning of shared facilities, security, waste skips. These costs go into a shared account and are split between the contractors in proportion to their share of the work.

An account manager, either one of the contractors or a third party, runs the account: budget, expenses with invoices, calls for funds and the final balance. A control committee of contractor representatives approves unplanned or disputed expenses. Each contractor sees its share and its calls for funds.

## Who it is for

Account managers and contractors who run shared site costs today with spreadsheets and email threads.

## What it is not

- Not legal or accounting advice. The app applies the rules set in each site's agreement.
- No real payments and no link with accounting software.

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#)
- Database: SQLite with EF Core

## Status

Early development. Setup instructions will be added as the code lands.

## License

[MIT](LICENSE)
