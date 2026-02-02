namespace Construct.Infrastructure.Persistence;

using Construct.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;



public class ConstructDbContext : IdentityDbContext<ApplicationUser>
{
    public ConstructDbContext(DbContextOptions<ConstructDbContext> options) : base(options) { }

    public DbSet<Bedrijf> Bedrijven => Set<Bedrijf>();
    public DbSet<Licentie> Licenties => Set<Licentie>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Bedrijf>()
            .HasMany(b => b.Gebruikers)
            .WithOne(u => u.Bedrijf!)
            .HasForeignKey(u => u.BedrijfId);

        builder.Entity<Bedrijf>()
            .HasOne(b => b.Licentie)
            .WithOne(l => l.Bedrijf!)
            .HasForeignKey<Licentie>(l => l.BedrijfId);
    }
}
