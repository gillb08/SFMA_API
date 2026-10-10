using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFMA_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignmentAndPublicInquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "InquiryMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "InquiryMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                table: "InquiryMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "Inquiries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoutedToId",
                table: "Inquiries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "Inquiries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "ChildName",
                table: "Inquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassApplying",
                table: "Inquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Inquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentEmail",
                table: "Inquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentName",
                table: "Inquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentPhone",
                table: "Inquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFile",
                table: "FeeTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "FeeTransactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "InquiryMessages");

            migrationBuilder.DropColumn(
                name: "SenderRole",
                table: "InquiryMessages");

            migrationBuilder.DropColumn(
                name: "ChildName",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "ClassApplying",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "ParentEmail",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "ParentName",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "ParentPhone",
                table: "Inquiries");

            migrationBuilder.DropColumn(
                name: "AttachmentFile",
                table: "FeeTransactions");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "FeeTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "InquiryMessages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "Inquiries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoutedToId",
                table: "Inquiries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "Inquiries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
