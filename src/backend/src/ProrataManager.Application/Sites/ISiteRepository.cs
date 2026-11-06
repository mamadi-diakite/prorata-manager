namespace ProrataManager.Application.Sites;

public interface ISiteRepository
{
    /// <returns>Sites of the organization, sorted by name, not tracked.</returns>
    Task<IReadOnlyList<Site>> ListAsync(Guid organizationId, CancellationToken cancellationToken);

    /// <returns>The site when it belongs to the organization, otherwise null.</returns>
    Task<Site?> FindAsync(Guid organizationId, Guid siteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Lot>> ListLotsAsync(IReadOnlyCollection<Guid> siteIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Contractor>> ListContractorsAsync(IReadOnlyCollection<Guid> siteIds, CancellationToken cancellationToken);

    void Add(Site site);

    void Add(Lot lot);

    void Add(Contractor contractor);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
