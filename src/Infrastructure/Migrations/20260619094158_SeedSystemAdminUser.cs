using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemAdminUser : Migration
    {
        // Intentionally empty (snapshot-only reconcile).
        //
        // The system-admin user is now seeded via HasData in OnModelCreating so
        // that SQLite EnsureCreated receives it. This migration exists only to
        // bring the model snapshot in sync with that HasData; it deliberately
        // does NOT insert the row, because every SQL Server database already
        // has it from 20260507131528_AddBootstrapRegistration's InsertData
        // (identity Id 1). Re-inserting Id 1 here would violate the primary
        // key on both fresh and existing SQL Server databases. The HasData Id
        // matches that existing row, so the Migrate path stays consistent.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
