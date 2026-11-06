namespace ProrataManager.Infrastructure.Seeding;

public sealed class DemoDataSeeder(ProrataManagerDbContext db, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "TestDemo123!";

    /// <summary>Seeds the <see cref="DemoData"/> organizations, their admins and their sites.</summary>
    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var passwordHash = passwordHasher.Hash(DemoPassword);

        foreach (var demo in DemoData.Organizations)
        {
            var organization = new ManagerOrganization(demo.Name, demo.Kind, now);
            db.Organizations.Add(organization);
            db.Users.Add(new User(organization.Id, demo.AdminEmail, demo.AdminName, passwordHash, UserRole.ManagerAdmin, now));

            foreach (var demoSite in demo.Sites)
            {
                AddSite(organization.Id, demoSite, now);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private void AddSite(Guid organizationId, DemoSite demo, DateTime now)
    {
        var site = new Site(
            organizationId, demo.Name, demo.Address, demo.ProjectOwner, demo.ProjectManager, demo.StartDate, demo.PlannedHandoverDate, now);
        site.ChangeStatus(demo.Status);
        db.Sites.Add(site);

        var contractors = new Dictionary<string, Contractor>();
        foreach (var demoContractor in demo.Contractors)
        {
            var contractor = new Contractor(
                site.Id,
                new ContractorDetails(demoContractor.CompanyName, ContactName: demoContractor.ContactName, ContactEmail: demoContractor.ContactEmail),
                now);
            contractors.Add(demoContractor.CompanyName, contractor);
            db.Contractors.Add(contractor);
        }

        foreach (var demoLot in demo.Lots)
        {
            var lot = new Lot(site.Id, demoLot.Number, demoLot.Name, demoLot.Family);
            lot.AssignTo(demoLot.Contractor is { } name ? contractors[name].Id : null);
            if (demoLot.ContractAmount is { } amount)
            {
                lot.SetContractAmount(amount);
            }

            db.Lots.Add(lot);
        }
    }
}
