using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Infrastructure.Persistence.Migrations
{
    public partial class BusinessFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WeekStartsOn = table.Column<int>(type: "int", nullable: false),
                    SlotMinutes = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                    table.CheckConstraint("CK_Businesses_SlotMinutes", "[SlotMinutes] IN (15, 30, 60)");
                    table.CheckConstraint("CK_Businesses_State", "[State] IN ('PendingApproval', 'Active', 'Rejected', 'Suspended', 'Archived')");
                    table.CheckConstraint("CK_Businesses_WeekStartsOn", "[WeekStartsOn] BETWEEN 0 AND 6");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemberships",
                columns: table => new
                {
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    State = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemberships", x => new { x.BusinessId, x.UserId });
                    table.CheckConstraint("CK_BusinessMemberships_RevokedAt", "([State] = 'Revoked' AND [RevokedAt] IS NOT NULL) OR ([State] <> 'Revoked' AND [RevokedAt] IS NULL)");
                    table.CheckConstraint("CK_BusinessMemberships_Role", "[Role] IN ('Owner', 'Manager')");
                    table.CheckConstraint("CK_BusinessMemberships_State", "[State] IN ('Pending', 'Active', 'Revoked')");
                    table.ForeignKey(
                        name: "FK_BusinessMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessMemberships_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessages_Attempts", "[Attempts] >= 0");
                    table.CheckConstraint("CK_OutboxMessages_Lease", "([State] = 'Processing' AND [LeaseId] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL) OR ([State] <> 'Processing' AND [LeaseId] IS NULL AND [LeaseExpiresAt] IS NULL)");
                    table.CheckConstraint("CK_OutboxMessages_Payload", "ISJSON([Payload]) = 1 AND DATALENGTH([Payload]) <= 16000");
                    table.CheckConstraint("CK_OutboxMessages_SentAt", "([State] = 'Sent' AND [SentAt] IS NOT NULL) OR ([State] <> 'Sent' AND [SentAt] IS NULL)");
                    table.CheckConstraint("CK_OutboxMessages_State", "[State] IN ('Pending', 'Processing', 'Sent', 'Failed')");
                    table.ForeignKey(
                        name: "FK_OutboxMessages_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublicReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationApplications", x => x.Id);
                    table.CheckConstraint("CK_RegistrationApplications_RejectionReason", "[State] <> 'Rejected' OR ([PublicReason] IS NOT NULL AND LEN(LTRIM(RTRIM([PublicReason]))) > 0)");
                    table.CheckConstraint("CK_RegistrationApplications_Review", "([State] = 'PendingReview' AND [ReviewedBy] IS NULL AND [ReviewedAt] IS NULL) OR ([State] IN ('Approved', 'Rejected') AND [ReviewedBy] IS NOT NULL AND [ReviewedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_RegistrationApplications_State", "[State] IN ('PendingReview', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_RegistrationApplications_AspNetUsers_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationApplications_BusinessMemberships_BusinessId_ApplicantUserId",
                        columns: x => new { x.BusinessId, x.ApplicantUserId },
                        principalTable: "BusinessMemberships",
                        principalColumns: new[] { "BusinessId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ActorUserId",
                table: "AuditEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_BusinessId_CreatedAt",
                table: "AuditEvents",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CreatedAt",
                table: "AuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_State_CreatedAt",
                table: "Businesses",
                columns: new[] { "State", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemberships_UserId_State_BusinessId",
                table: "BusinessMemberships",
                columns: new[] { "UserId", "State", "BusinessId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_BusinessId",
                table: "OutboxMessages",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeduplicationKey",
                table: "OutboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_State_LeaseExpiresAt",
                table: "OutboxMessages",
                columns: new[] { "State", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_State_NextAttemptAt",
                table: "OutboxMessages",
                columns: new[] { "State", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_ApplicantUserId",
                table: "RegistrationApplications",
                column: "ApplicantUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_BusinessId",
                table: "RegistrationApplications",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_BusinessId_ApplicantUserId",
                table: "RegistrationApplications",
                columns: new[] { "BusinessId", "ApplicantUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_ReviewedBy",
                table: "RegistrationApplications",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationApplications_State_CreatedAt",
                table: "RegistrationApplications",
                columns: new[] { "State", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "RegistrationApplications");

            migrationBuilder.DropTable(
                name: "BusinessMemberships");

            migrationBuilder.DropTable(
                name: "Businesses");
        }
    }
}
