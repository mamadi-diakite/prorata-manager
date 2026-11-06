namespace ProrataManager.Domain.Tests;

public sealed class SiteTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Creates_a_site_in_preparation()
    {
        var site = NewSite();

        Assert.NotEqual(Guid.Empty, site.Id);
        Assert.Equal(OrganizationId, site.OrganizationId);
        Assert.Equal("Linden Court housing", site.Name);
        Assert.Equal("12 Avenue des Tilleuls, Lyon", site.Address);
        Assert.Equal("Cedar Homes", site.ProjectOwner);
        Assert.Equal("North Arc Architects", site.ProjectManager);
        Assert.Equal(TestDates.Start, site.StartDate);
        Assert.Equal(TestDates.Handover, site.PlannedHandoverDate);
        Assert.Equal(SiteStatus.Preparation, site.Status);
        Assert.Equal(TestDates.Now, site.CreatedAtUtc);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("organizationId", Assert.Throws<ArgumentException>(() => new Site(
            Guid.Empty, "Site", "Address", "Owner", "Manager", TestDates.Start, TestDates.Handover, TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(() => new Site(
            OrganizationId, "Site", "Address", "Owner", "Manager", TestDates.Start, TestDates.Handover, TestDates.Local)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new Site(
            OrganizationId, " ", "Address", "Owner", "Manager", TestDates.Start, TestDates.Handover, TestDates.Now)).ParamName);
        Assert.Equal("address", Assert.Throws<ArgumentException>(() => new Site(
            OrganizationId, "Site", " ", "Owner", "Manager", TestDates.Start, TestDates.Handover, TestDates.Now)).ParamName);
        Assert.Equal("projectOwner", Assert.Throws<ArgumentException>(() => new Site(
            OrganizationId, "Site", "Address", " ", "Manager", TestDates.Start, TestDates.Handover, TestDates.Now)).ParamName);
        Assert.Equal("projectManager", Assert.Throws<ArgumentException>(() => new Site(
            OrganizationId, "Site", "Address", "Owner", " ", TestDates.Start, TestDates.Handover, TestDates.Now)).ParamName);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void Planned_handover_must_be_after_the_start(int daysAfterStart, bool expected)
    {
        var handover = TestDates.Start.AddDays(daysAfterStart);

        Assert.Equal(expected, Site.IsValidSchedule(TestDates.Start, handover));
        if (!expected)
        {
            Assert.Equal("plannedHandoverDate", Assert.Throws<ArgumentException>(() => new Site(
                OrganizationId, "Site", "Address", "Owner", "Manager", TestDates.Start, handover, TestDates.Now)).ParamName);
        }
    }

    [Fact]
    public void Update_details_replaces_every_field()
    {
        var site = NewSite();

        site.UpdateDetails(" Riverside offices ", " 3 Quai Sud ", " Owner 2 ", " Manager 2 ", new DateOnly(2027, 3, 1), new DateOnly(2028, 9, 1));

        Assert.Equal("Riverside offices", site.Name);
        Assert.Equal("3 Quai Sud", site.Address);
        Assert.Equal("Owner 2", site.ProjectOwner);
        Assert.Equal("Manager 2", site.ProjectManager);
        Assert.Equal(new DateOnly(2027, 3, 1), site.StartDate);
        Assert.Equal(new DateOnly(2028, 9, 1), site.PlannedHandoverDate);
    }

    [Fact]
    public void Update_details_changes_nothing_when_a_value_is_invalid()
    {
        var site = NewSite();

        Assert.Throws<ArgumentException>(
            () => site.UpdateDetails("Renamed", "Address", "Owner", "Manager", TestDates.Handover, TestDates.Start));

        Assert.Equal("Linden Court housing", site.Name);
        Assert.Equal(TestDates.Start, site.StartDate);
    }

    [Theory]
    [InlineData(SiteStatus.Preparation, SiteStatus.Preparation, true)]
    [InlineData(SiteStatus.Preparation, SiteStatus.InProgress, true)]
    [InlineData(SiteStatus.Preparation, SiteStatus.Closed, true)]
    [InlineData(SiteStatus.HandedOver, SiteStatus.Closed, true)]
    [InlineData(SiteStatus.InProgress, SiteStatus.Preparation, false)]
    [InlineData(SiteStatus.Closed, SiteStatus.HandedOver, false)]
    [InlineData(SiteStatus.Preparation, (SiteStatus)9, false)]
    public void Status_only_moves_forward(SiteStatus from, SiteStatus to, bool expected)
    {
        Assert.Equal(expected, Site.CanMove(from, to));
    }

    [Fact]
    public void Change_status_moves_forward_and_refuses_to_go_back()
    {
        var site = NewSite();

        site.ChangeStatus(SiteStatus.InProgress);
        Assert.Equal(SiteStatus.InProgress, site.Status);

        var error = Assert.Throws<InvalidOperationException>(() => site.ChangeStatus(SiteStatus.Preparation));
        Assert.Equal("A site cannot move from InProgress to Preparation.", error.Message);
        Assert.Equal(SiteStatus.InProgress, site.Status);
    }

    private static Site NewSite() => new(
        OrganizationId,
        " Linden Court housing ",
        "12 Avenue des Tilleuls, Lyon",
        "Cedar Homes",
        "North Arc Architects",
        TestDates.Start,
        TestDates.Handover,
        TestDates.Now);
}
