namespace ProrataManager.Infrastructure.Persistence;

public sealed class ProrataManagerDbContext(DbContextOptions<ProrataManagerDbContext> options) : DbContext(options)
{
    public DbSet<ManagerOrganization> Organizations => Set<ManagerOrganization>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Lot> Lots => Set<Lot>();

    public DbSet<Contractor> Contractors => Set<Contractor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProrataManagerDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
