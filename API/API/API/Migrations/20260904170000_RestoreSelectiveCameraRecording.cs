using API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations;

/// <summary>
/// Restores the 03/09 selective recording policy. The later always-on migration
/// occupied direct RTSP sessions that must remain available to Python AI.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904170000_RestoreSelectiveCameraRecording")]
public partial class RestoreSelectiveCameraRecording : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE [Camera] SET [IsRecordingEnabled] = 0;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not automatically re-enable direct RTSP recorders.
    }
}
