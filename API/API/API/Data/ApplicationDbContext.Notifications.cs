using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRule> NotificationRules { get; set; }

    public void ConfigureNotificationModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.RecipientUser)
                  .WithMany()
                  .HasForeignKey(e => e.RecipientUserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_Notification_RecipientUser");

            entity.HasIndex(e => new { e.RecipientUserId, e.IsRead, e.CreatedAt });
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Latitude).HasColumnType("decimal(18,12)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(18,12)");
        });

        modelBuilder.Entity<NotificationRule>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(e => e.RecipientUser)
                  .WithMany()
                  .HasForeignKey(e => e.RecipientUserId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_NotificationRule_RecipientUser");

            entity.HasIndex(e => e.EventType);
        });
    }
}
