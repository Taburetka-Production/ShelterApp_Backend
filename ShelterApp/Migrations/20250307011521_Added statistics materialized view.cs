using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Migrations
{
    /// <inheritdoc />
    public partial class Addedstatisticsmaterializedview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW totalstatistics AS
                SELECT 
                    (SELECT COUNT(*) FROM ""Shelters"") AS totalshelters,
                    (SELECT COUNT(*) FROM ""Animals"") AS totalanimals,
                    (SELECT COUNT(*) FROM ""AspNetUsers"") AS totalusers,
                    (SELECT COUNT(DISTINCT ""Region"") FROM ""Addresses"") AS totalregions,
	                (SELECT COUNT(*) FROM ""AdoptionRequests"") AS totaladoptions;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS totalstatistics;");
        }
    }
}
