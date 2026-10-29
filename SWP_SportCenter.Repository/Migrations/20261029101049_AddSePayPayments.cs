using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSePayPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FIX SEPAY: thêm snapshot chỉ dẫn và sổ webhook, không đổi trạng thái hóa đơn/thẻ cũ.
            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "invoice",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SePayAccountNumber",
                table: "invoice",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SePayBankBin",
                table: "invoice",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SePayGateway",
                table: "invoice",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SePayQrCodeUrl",
                table: "invoice",
                type: "character varying(1200)",
                maxLength: 1200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sepay_webhook_receipt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubAccount = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Gateway = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankTransactionDate = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sepay_webhook_receipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sepay_webhook_receipt_invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoice",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_PaymentReference",
                table: "invoice",
                column: "PaymentReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sepay_webhook_receipt_InvoiceId",
                table: "sepay_webhook_receipt",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_sepay_webhook_receipt_Outcome",
                table: "sepay_webhook_receipt",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_sepay_webhook_receipt_ReceivedAt",
                table: "sepay_webhook_receipt",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FIX SEPAY: rollback xóa receipt/snapshot phát sinh; phải backup trước khi dùng trên DB thật.
            migrationBuilder.DropTable(
                name: "sepay_webhook_receipt");

            migrationBuilder.DropIndex(
                name: "IX_invoice_PaymentReference",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "SePayAccountNumber",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "SePayBankBin",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "SePayGateway",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "SePayQrCodeUrl",
                table: "invoice");
        }
    }
}
