using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ScarletPigsServices.Data.Auth;
using ScarletPigsServices.Data.Events;

namespace ScarletPigsServices.Data
{
    public class ScarletPigsDbContext : IdentityDbContext<ApplicationUser>
    {
        public ScarletPigsDbContext(DbContextOptions<ScarletPigsDbContext> options) : base(options)
        {

        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventType> EventTypes => Set<EventType>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(refreshToken =>
            {
                refreshToken.ToTable("RefreshTokens");
                refreshToken.HasKey(token => token.Id);
                refreshToken.Property(token => token.TokenHash)
                    .HasMaxLength(64)
                    .IsRequired();
                refreshToken.Property(token => token.SecurityStamp)
                    .HasMaxLength(256)
                    .IsRequired();
                refreshToken.Property(token => token.ConcurrencyStamp)
                    .IsConcurrencyToken();
                refreshToken.HasIndex(token => token.TokenHash)
                    .IsUnique();
                refreshToken.HasIndex(token => token.UserId);
                refreshToken.HasOne(token => token.User)
                    .WithMany(user => user.RefreshTokens)
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "4b4ebf60-4048-41c2-a14f-cd4f98716fa2",
                    Name = AuthRoles.UnitOrganizer,
                    NormalizedName = AuthRoles.UnitOrganizer.ToUpperInvariant(),
                    ConcurrencyStamp = "b8a82f98-dd1a-43e8-b14f-17ec8c85313c"
                },
                new IdentityRole
                {
                    Id = "2cf9d6c9-c479-4e96-b48e-a4f6664f31df",
                    Name = AuthRoles.MissionMaker,
                    NormalizedName = AuthRoles.MissionMaker.ToUpperInvariant(),
                    ConcurrencyStamp = "0146fa76-107f-48f4-8400-8e723ce4cc5c"
                });
        }
    }
}
