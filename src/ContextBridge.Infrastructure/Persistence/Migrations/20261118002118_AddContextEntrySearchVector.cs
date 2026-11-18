using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContextBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContextEntrySearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE context_entries ADD COLUMN search_vector tsvector
                    GENERATED ALWAYS AS (to_tsvector('simple', coalesce("Title", '') || ' ' || coalesce("Content", ''))) STORED;
                """);

            migrationBuilder.Sql(
                "CREATE INDEX ix_context_entries_search_vector ON context_entries USING GIN (search_vector);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_context_entries_search_vector;");
            migrationBuilder.Sql("ALTER TABLE context_entries DROP COLUMN IF EXISTS search_vector;");
        }
    }
}
