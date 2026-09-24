using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFMA_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentBudgetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentBudgets_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentBudgets_AcademicTermId",
                table: "DepartmentBudgets",
                column: "AcademicTermId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentBudgets");
        }
    }
}
