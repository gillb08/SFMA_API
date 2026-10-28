using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFMA_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicCurriculumWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "SchemeOfWorkEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByStaffId",
                table: "SchemeOfWorkEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumTopicId",
                table: "SchemeOfWorkEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "SchemeOfWorkEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "SchemeOfWorkEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VettedAt",
                table: "SchemeOfWorkEntries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VettedByStaffId",
                table: "SchemeOfWorkEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicTermId",
                table: "LessonNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "LessonNotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRemark",
                table: "LessonNotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "LessonNotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LessonDate",
                table: "LessonNotes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodNumber",
                table: "LessonNotes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchemeOfWorkEntryId",
                table: "LessonNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "LessonNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicWorkflowAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: true),
                    ToStatus = table.Column<string>(type: "text", nullable: true),
                    ActorStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorName = table.Column<string>(type: "text", nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicWorkflowAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicWorkflowAuditLogs_Staff_ActorStaffId",
                        column: x => x.ActorStaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: false),
                    LearningOutcomes = table.Column<string>(type: "text", nullable: true),
                    SequenceNumber = table.Column<short>(type: "smallint", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_ClassSections_ClassSectionId",
                        column: x => x.ClassSectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorkEntries_CreatedByStaffId",
                table: "SchemeOfWorkEntries",
                column: "CreatedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorkEntries_CurriculumTopicId",
                table: "SchemeOfWorkEntries",
                column: "CurriculumTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorkEntries_SubjectId",
                table: "SchemeOfWorkEntries",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorkEntries_VettedByStaffId",
                table: "SchemeOfWorkEntries",
                column: "VettedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonNotes_AcademicTermId",
                table: "LessonNotes",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonNotes_SchemeOfWorkEntryId",
                table: "LessonNotes",
                column: "SchemeOfWorkEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonNotes_SubjectId",
                table: "LessonNotes",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicWorkflowAuditLogs_ActorStaffId",
                table: "AcademicWorkflowAuditLogs",
                column: "ActorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_AcademicTermId",
                table: "CurriculumTopics",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_ClassSectionId",
                table: "CurriculumTopics",
                column: "ClassSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SubjectId",
                table: "CurriculumTopics",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonNotes_AcademicTerms_AcademicTermId",
                table: "LessonNotes",
                column: "AcademicTermId",
                principalTable: "AcademicTerms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonNotes_SchemeOfWorkEntries_SchemeOfWorkEntryId",
                table: "LessonNotes",
                column: "SchemeOfWorkEntryId",
                principalTable: "SchemeOfWorkEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonNotes_Subjects_SubjectId",
                table: "LessonNotes",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SchemeOfWorkEntries_CurriculumTopics_CurriculumTopicId",
                table: "SchemeOfWorkEntries",
                column: "CurriculumTopicId",
                principalTable: "CurriculumTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SchemeOfWorkEntries_Staff_CreatedByStaffId",
                table: "SchemeOfWorkEntries",
                column: "CreatedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SchemeOfWorkEntries_Staff_VettedByStaffId",
                table: "SchemeOfWorkEntries",
                column: "VettedByStaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SchemeOfWorkEntries_Subjects_SubjectId",
                table: "SchemeOfWorkEntries",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill AcademicTermId in LessonNotes from ClassSections
            migrationBuilder.Sql(@"
                UPDATE ""LessonNotes"" ln
                SET ""AcademicTermId"" = cs.""AcademicTermId""
                FROM ""ClassSections"" cs
                WHERE ln.""ClassSectionId"" = cs.""Id"" AND ln.""AcademicTermId"" IS NULL;
            ");

            // Backfill ApprovalStatus in SchemeOfWorkEntries (Draft = 1, Approved = 3)
            migrationBuilder.Sql(@"
                UPDATE ""SchemeOfWorkEntries""
                SET ""ApprovalStatus"" = CASE WHEN ""VettedByHos"" = TRUE THEN 3 ELSE 1 END
                WHERE ""ApprovalStatus"" = 0;
            ");

            // Backfill DeliveryStatus in LessonNotes (Planned = 1)
            migrationBuilder.Sql(@"
                UPDATE ""LessonNotes""
                SET ""DeliveryStatus"" = 1
                WHERE ""DeliveryStatus"" = 0;
            ");

            // Backfill LessonNotes SubjectId from exact name match in Subjects table
            migrationBuilder.Sql(@"
                UPDATE ""LessonNotes"" ln
                SET ""SubjectId"" = s.""Id""
                FROM ""Subjects"" s
                WHERE LOWER(TRIM(ln.""Subject"")) = LOWER(TRIM(s.""Name""))
                  AND ln.""SubjectId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonNotes_AcademicTerms_AcademicTermId",
                table: "LessonNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonNotes_SchemeOfWorkEntries_SchemeOfWorkEntryId",
                table: "LessonNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonNotes_Subjects_SubjectId",
                table: "LessonNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_SchemeOfWorkEntries_CurriculumTopics_CurriculumTopicId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SchemeOfWorkEntries_Staff_CreatedByStaffId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SchemeOfWorkEntries_Staff_VettedByStaffId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SchemeOfWorkEntries_Subjects_SubjectId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropTable(
                name: "AcademicWorkflowAuditLogs");

            migrationBuilder.DropTable(
                name: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_SchemeOfWorkEntries_CreatedByStaffId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_SchemeOfWorkEntries_CurriculumTopicId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_SchemeOfWorkEntries_SubjectId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_SchemeOfWorkEntries_VettedByStaffId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_LessonNotes_AcademicTermId",
                table: "LessonNotes");

            migrationBuilder.DropIndex(
                name: "IX_LessonNotes_SchemeOfWorkEntryId",
                table: "LessonNotes");

            migrationBuilder.DropIndex(
                name: "IX_LessonNotes_SubjectId",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByStaffId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "CurriculumTopicId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "VettedAt",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "VettedByStaffId",
                table: "SchemeOfWorkEntries");

            migrationBuilder.DropColumn(
                name: "AcademicTermId",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "DeliveryRemark",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "LessonDate",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "PeriodNumber",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "SchemeOfWorkEntryId",
                table: "LessonNotes");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "LessonNotes");
        }
    }
}
