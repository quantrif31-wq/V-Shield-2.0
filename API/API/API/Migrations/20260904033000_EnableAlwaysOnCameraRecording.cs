using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations;

/// <summary>
/// Moves existing camera inventory to the always-on recording policy.
/// </summary>
public partial class EnableAlwaysOnCameraRecording : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE [Camera] SET [IsRecordingEnabled] = 1 WHERE [IsRecordingEnabled] = 0;");
        migrationBuilder.Sql("UPDATE [Camera] SET [RecordingRetentionDays] = 30 WHERE [RecordingRetentionDays] <= 0;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Existing camera choices must not be destructively reverted.
    }
}
