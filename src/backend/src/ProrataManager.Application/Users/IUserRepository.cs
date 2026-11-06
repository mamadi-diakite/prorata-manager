namespace ProrataManager.Application.Users;

public interface IUserRepository
{
    /// <param name="email">Normalized email: trimmed and lowercase.</param>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ManagerOrganization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);

    void AddOrganization(ManagerOrganization organization);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
