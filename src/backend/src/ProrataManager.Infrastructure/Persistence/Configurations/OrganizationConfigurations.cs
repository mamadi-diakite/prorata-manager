namespace ProrataManager.Infrastructure.Persistence.Configurations;

internal sealed class ManagerOrganizationConfiguration : IEntityTypeConfiguration<ManagerOrganization>
{
    public void Configure(EntityTypeBuilder<ManagerOrganization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(ManagerOrganization.NameMaxLength).IsRequired();
        builder.Property(o => o.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.RegistrationNumber).HasMaxLength(ManagerOrganization.RegistrationNumberMaxLength);
        builder.Property(o => o.Address).HasMaxLength(ManagerOrganization.AddressMaxLength);
        builder.Property(o => o.Phone).HasMaxLength(ManagerOrganization.PhoneMaxLength);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(User.PasswordHashMaxLength).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<ManagerOrganization>()
            .WithMany()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
