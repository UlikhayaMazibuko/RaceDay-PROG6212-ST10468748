using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Disciplines",
                columns: table => new
                {
                    DisciplineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisciplineName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disciplines", x => x.DisciplineId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Dob", "[DateOfBirth] < CAST(GETDATE() AS DATE)");
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserId = table.Column<int>(type: "int", nullable: false),
                    DisciplineId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    RegistrationCloseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_CloseDate", "[RegistrationCloseDate] <= [EventDate]");
                    table.CheckConstraint("CK_Events_Status", "[Status] IN ('Draft', 'Published', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Events_Discipline",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "DisciplineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Organiser",
                        column: x => x.OrganiserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(8,2)", nullable: false, defaultValue: 0m),
                    CategoryStartTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true),
                    MinimumAge = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.CategoryId);
                    table.CheckConstraint("CK_EventCategories_Age", "[MinimumAge] >= 0");
                    table.CheckConstraint("CK_EventCategories_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_EventCategories_Fee", "[EntryFee] >= 0");
                    table.CheckConstraint("CK_EventCategories_Max", "[MaxParticipants] IS NULL OR [MaxParticipants] > 0");
                    table.ForeignKey(
                        name: "FK_EventCategories_Event",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    RaceNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EnrolmentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Category",
                        column: x => x.CategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Participant",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    CapturedByUserId = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    PositionOverall = table.Column<int>(type: "int", nullable: true),
                    PositionInCategory = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, defaultValue: "Finished"),
                    CapturedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.CheckConstraint("CK_Results_Category", "[PositionInCategory] IS NULL OR [PositionInCategory] > 0");
                    table.CheckConstraint("CK_Results_Overall", "[PositionOverall] IS NULL OR [PositionOverall] > 0");
                    table.CheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'DNF', 'DNS', 'DQ')");
                    table.ForeignKey(
                        name: "FK_Results_CapturedBy",
                        column: x => x.CapturedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Results_Enrolment",
                        column: x => x.EnrolmentId,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Disciplines",
                columns: new[] { "DisciplineId", "DisciplineName" },
                values: new object[,]
                {
                    { 1, "Running" },
                    { 2, "Walking" },
                    { 3, "Cycling" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Description", "RoleName" },
                values: new object[,]
                {
                    { 1, "Creates and manages events, categories, enrolments and results.", "Organiser" },
                    { 2, "Browses events, enters events and tracks personal results.", "Participant" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Disciplines_Name",
                table: "Disciplines",
                column: "DisciplineName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_ParticipantId",
                table: "Enrolments",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_Participant",
                table: "Enrolments",
                columns: new[] { "CategoryId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_RaceNumber",
                table: "Enrolments",
                columns: new[] { "CategoryId", "RaceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EventCategories_Name",
                table: "EventCategories",
                columns: new[] { "EventId", "CategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_DisciplineId",
                table: "Events",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserId",
                table: "Events",
                column: "OrganiserId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CapturedByUserId",
                table: "Results",
                column: "CapturedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Results_Enrolment",
                table: "Results",
                column: "EnrolmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_Name",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Disciplines");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
