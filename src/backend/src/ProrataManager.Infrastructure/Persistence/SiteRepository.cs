namespace ProrataManager.Infrastructure.Persistence;

public sealed class SiteRepository(ProrataManagerDbContext db) : ISiteRepository
{
    public async Task<IReadOnlyList<Site>> ListAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await db.Sites
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    public Task<Site?> FindAsync(Guid organizationId, Guid siteId, CancellationToken cancellationToken) =>
        db.Sites.SingleOrDefaultAsync(s => s.Id == siteId && s.OrganizationId == organizationId, cancellationToken);

    public async Task<IReadOnlyList<Lot>> ListLotsAsync(IReadOnlyCollection<Guid> siteIds, CancellationToken cancellationToken) =>
        await db.Lots.Where(l => siteIds.Contains(l.SiteId)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Contractor>> ListContractorsAsync(IReadOnlyCollection<Guid> siteIds, CancellationToken cancellationToken) =>
        await db.Contractors.Where(c => siteIds.Contains(c.SiteId)).ToListAsync(cancellationToken);

    public void Add(Site site) => db.Sites.Add(site);

    public void Add(Lot lot) => db.Lots.Add(lot);

    public void Add(Contractor contractor) => db.Contractors.Add(contractor);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
