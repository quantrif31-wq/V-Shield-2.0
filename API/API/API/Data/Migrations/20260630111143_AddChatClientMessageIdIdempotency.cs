using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatClientMessageIdIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MfaRecoveryCodes_AppUsers_UserId",
                table: "MfaRecoveryCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOperationalScopes_AppUsers_UserId",
                table: "UserOperationalScopes");

            migrationBuilder.AddColumn<string>(
                name: "ClientMessageId",
                table: "ChatMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_SenderId_ClientMessageId",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "SenderId", "ClientMessageId" },
                unique: true,
                filter: "[ClientMessageId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MfaRecoveryCodes_AppUsers_UserId",
                table: "MfaRecoveryCodes",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOperationalScopes_AppUsers_UserId",
                table: "UserOperationalScopes",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MfaRecoveryCodes_AppUsers_UserId",
                table: "MfaRecoveryCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOperationalScopes_AppUsers_UserId",
                table: "UserOperationalScopes");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ConversationId_SenderId_ClientMessageId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ClientMessageId",
                table: "ChatMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_MfaRecoveryCodes_AppUsers_UserId",
                table: "MfaRecoveryCodes",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOperationalScopes_AppUsers_UserId",
                table: "UserOperationalScopes",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
