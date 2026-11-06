namespace ProrataManager.Infrastructure.Persistence.Configurations;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(Site.NameMaxLength).IsRequired();
        builder.Property(s => s.Address).HasMaxLength(Site.AddressMaxLength).IsRequired();
        builder.Property(s => s.ProjectOwner).HasMaxLength(Site.PartyNameMaxLength).IsRequired();
        builder.Property(s => s.ProjectManager).HasMaxLength(Site.PartyNameMaxLength).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(s => s.OrganizationId);

        builder.HasOne<ManagerOrganization>()
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.ToTable("Lots");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Number).HasMaxLength(Lot.NumberMaxLength).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(Lot.NameMaxLength).IsRequired();
        builder.Property(l => l.Family).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.ContractAmount).HasPrecision(12, 2);
        builder.HasIndex(l => new { l.SiteId, l.Number }).IsUnique();

        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(l => l.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Contractor>()
            .WithMany()
            .HasForeignKey(l => l.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("Contractors");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyName).HasMaxLength(Contractor.CompanyNameMaxLength).IsRequired();
        builder.Property(c => c.RegistrationNumber).HasMaxLength(Contractor.RegistrationNumberMaxLength);
        builder.Property(c => c.Address).HasMaxLength(Contractor.AddressMaxLength);
        builder.Property(c => c.ContactName).HasMaxLength(Contractor.ContactNameMaxLength);
        builder.Property(c => c.ContactEmail).HasMaxLength(Contractor.EmailMaxLength);
        builder.Property(c => c.Phone).HasMaxLength(Contractor.PhoneMaxLength);
        builder.HasIndex(c => c.SiteId);

        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(c => c.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
