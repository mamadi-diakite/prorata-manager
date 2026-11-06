namespace ProrataManager.Application.Tests;

internal static class TestClock
{
    public static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
}

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public List<ManagerOrganization> Organizations { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public Task<ManagerOrganization?> FindOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Organizations.SingleOrDefault(o => o.Id == id));

    public void Add(User user) => Users.Add(user);

    public void AddOrganization(ManagerOrganization organization) => Organizations.Add(organization);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Readable fake: the hash is the password with a prefix.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCount { get; private set; }

    public string Hash(string password)
    {
        HashCount++;
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAt = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    public AccessToken Issue(User user) => new($"token-for-{user.Id}", ExpiresAt);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
