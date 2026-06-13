using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public partial class ApplicationDbContext
{
    public DbSet<AiAnalysisJob> AiAnalysisJobs { get; set; }
    public DbSet<AiModelRun> AiModelRuns { get; set; }
    public DbSet<AiRecommendation> AiRecommendations { get; set; }
    public DbSet<AiRecommendationEvidence> AiRecommendationEvidences { get; set; }
    public DbSet<AiFeedback> AiFeedbacks { get; set; }
    public DbSet<AiEventMetadata> AiEventMetadataSet { get; set; }

    private static void ConfigureAiCore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiAnalysisJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.InputSummary).HasMaxLength(2000);
            entity.Property(e => e.ErrorCode).HasMaxLength(500);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.Status, e.JobType, e.CreatedAtUtc });
            entity.HasIndex(e => e.CorrelationId).IsUnique();
        });

        modelBuilder.Entity<AiModelRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(80);
            entity.Property(e => e.PromptTemplateKey).IsRequired().HasMaxLength(120);
            entity.Property(e => e.InputHash).HasMaxLength(128);
            entity.Property(e => e.OutputHash).HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.HasOne(e => e.AnalysisJob)
                .WithMany(j => j.ModelRuns)
                .HasForeignKey(e => e.AnalysisJobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.AnalysisJobId, e.CreatedAtUtc });
        });

        modelBuilder.Entity<AiRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(80);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ReasoningSummary).HasMaxLength(4000);
            entity.Property(e => e.RecommendedAction).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(40);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.HasOne(e => e.AnalysisJob)
                .WithMany(j => j.Recommendations)
                .HasForeignKey(e => e.AnalysisJobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.Domain, e.EntityType, e.EntityId });
            entity.HasIndex(e => new { e.Status, e.Severity, e.CreatedAtUtc });
        });

        modelBuilder.Entity<AiRecommendationEvidence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.SourceId).HasMaxLength(120);
            entity.Property(e => e.Snippet).HasMaxLength(2000);
            entity.Property(e => e.Weight).HasColumnType("decimal(5,2)").HasDefaultValue(1.0m);
            entity.HasOne(e => e.Recommendation)
                .WithMany(r => r.Evidence)
                .HasForeignKey(e => e.RecommendationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.RecommendationId);
        });

        modelBuilder.Entity<AiFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeedbackType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Comment).HasMaxLength(2000);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.RecommendationId, e.UserId });
        });

        modelBuilder.Entity<AiEventMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.SourceId).HasMaxLength(120);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.SubjectType).HasMaxLength(80);
            entity.Property(e => e.SubjectId).HasMaxLength(120);
            entity.Property(e => e.ObjectType).HasMaxLength(80);
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.Property(e => e.Confidence).HasColumnType("decimal(9,6)");
            entity.Property(e => e.ModelName).HasMaxLength(80);
            entity.Property(e => e.ModelVersion).HasMaxLength(80);
            entity.Property(e => e.RawMetadataJson).HasMaxLength(4000);
            entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(getutcdate())");
            entity.HasIndex(e => new { e.SourceType, e.EventType, e.OccurredAtUtc });
            entity.HasIndex(e => e.CorrelationId);
        });
    }
}
