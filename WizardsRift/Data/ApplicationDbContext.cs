using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WizardsRift.Data;

using WizardsRift.Models;

public class ApplicationDbContext : IdentityDbContext
{
    public DbSet<Mod> Mods { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Mod>()
            .HasIndex(m => m.Name)
            .IsUnique();

        builder.Entity<Mod>()
            .HasOne(m => m.Author)
            .WithMany(u => u.Mods);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Mods)
            .WithOne(m => m.Author);
    }
}