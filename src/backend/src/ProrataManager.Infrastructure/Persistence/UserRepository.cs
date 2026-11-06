namespace ProrataManager.Infrastructure.Persistence;

public sealed class UserRepository(ProrataManagerDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<ManagerOrganization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        db.Organizations.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void AddOrganization(ManagerOrganization organization) => db.Organizations.Add(organization);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
