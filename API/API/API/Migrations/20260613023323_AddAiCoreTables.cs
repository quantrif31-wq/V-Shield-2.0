using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAnalysisJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiEventMetadataSet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: true),
                    ZoneId = table.Column<int>(type: "int", nullable: true),
                    CameraId = table.Column<int>(type: "int", nullable: true),
                    GateId = table.Column<int>(type: "int", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SubjectId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RawMetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEventMetadataSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiFeedbacks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FeedbackType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiModelRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisJobId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PromptTemplateKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PromptTemplateVersion = table.Column<int>(type: "int", nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OutputHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    TokenEstimate = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModelRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiModelRuns_AiAnalysisJobs_AnalysisJobId",
                        column: x => x.AnalysisJobId,
                        principalTable: "AiAnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiRecommendations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisJobId = table.Column<long>(type: "bigint", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReasoningSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RecommendedAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresHumanApproval = table.Column<bool>(type: "bit", nullable: false),
                    RequiresStepUp = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiRecommendations_AiAnalysisJobs_AnalysisJobId",
                        column: x => x.AnalysisJobId,
                        principalTable: "AiAnalysisJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiRecommendationEvidences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SourceTimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Snippet = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRecommendationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiRecommendationEvidences_AiRecommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "AiRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisJobs_CorrelationId",
                table: "AiAnalysisJobs",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisJobs_Status_JobType_CreatedAtUtc",
                table: "AiAnalysisJobs",
                columns: new[] { "Status", "JobType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiEventMetadataSet_CorrelationId",
                table: "AiEventMetadataSet",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEventMetadataSet_SourceType_EventType_OccurredAtUtc",
                table: "AiEventMetadataSet",
                columns: new[] { "SourceType", "EventType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_RecommendationId_UserId",
                table: "AiFeedbacks",
                columns: new[] { "RecommendationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiModelRuns_AnalysisJobId_CreatedAtUtc",
                table: "AiModelRuns",
                columns: new[] { "AnalysisJobId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendationEvidences_RecommendationId",
                table: "AiRecommendationEvidences",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendations_AnalysisJobId",
                table: "AiRecommendations",
                column: "AnalysisJobId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendations_Domain_EntityType_EntityId",
                table: "AiRecommendations",
                columns: new[] { "Domain", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendations_Status_Severity_CreatedAtUtc",
                table: "AiRecommendations",
                columns: new[] { "Status", "Severity", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEventMetadataSet");

            migrationBuilder.DropTable(
                name: "AiFeedbacks");

            migrationBuilder.DropTable(
                name: "AiModelRuns");

            migrationBuilder.DropTable(
                name: "AiRecommendationEvidences");

            migrationBuilder.DropTable(
                name: "AiRecommendations");

            migrationBuilder.DropTable(
                name: "AiAnalysisJobs");
        }
    }
}
