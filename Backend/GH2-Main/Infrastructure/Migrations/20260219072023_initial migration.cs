using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssetType = table.Column<string>(type: "text", nullable: false),
                    ParentAssetId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.AssetId);
                });

            migrationBuilder.CreateTable(
                name: "TagTypes",
                columns: table => new
                {
                    TagTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagTypes", x => x.TagTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagTypeId = table.Column<int>(type: "integer", nullable: false),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    LowerLimit = table.Column<float>(type: "real", nullable: false),
                    UpperLimit = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                    table.ForeignKey(
                        name: "FK_Tags_TagTypes_TagTypeId",
                        column: x => x.TagTypeId,
                        principalTable: "TagTypes",
                        principalColumn: "TagTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mappings",
                columns: table => new
                {
                    MappingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    OpcNodeId = table.Column<string>(type: "text", nullable: false),
                    RegisterAddress = table.Column<int>(type: "integer", nullable: false),
                    DataType = table.Column<string>(type: "text", nullable: false),
                    RegisterCount = table.Column<int>(type: "integer", nullable: false),
                    Deadband = table.Column<int>(type: "integer", nullable: false),
                    FunctionCode = table.Column<int>(type: "integer", nullable: false),
                    SlaveId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mappings", x => x.MappingId);
                    table.ForeignKey(
                        name: "FK_Mappings_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Mappings_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeLastDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MappingId = table.Column<int>(type: "integer", nullable: false),
                    OpcNodeId = table.Column<string>(type: "text", nullable: false),
                    AssetName = table.Column<string>(type: "text", nullable: false),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeLastDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeLastDatas_Mappings_MappingId",
                        column: x => x.MappingId,
                        principalTable: "Mappings",
                        principalColumn: "MappingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensorRawDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MappingId = table.Column<int>(type: "integer", nullable: false),
                    OpcNodeId = table.Column<string>(type: "text", nullable: false),
                    AssetName = table.Column<string>(type: "text", nullable: false),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorRawDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorRawDatas_Mappings_MappingId",
                        column: x => x.MappingId,
                        principalTable: "Mappings",
                        principalColumn: "MappingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MappingId = table.Column<int>(type: "integer", nullable: false),
                    OpcNodeId = table.Column<string>(type: "text", nullable: false),
                    AssetName = table.Column<string>(type: "text", nullable: false),
                    TagName = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionData_Mappings_MappingId",
                        column: x => x.MappingId,
                        principalTable: "Mappings",
                        principalColumn: "MappingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mappings_AssetId",
                table: "Mappings",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Mappings_TagId",
                table: "Mappings",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeLastDatas_MappingId",
                table: "NodeLastDatas",
                column: "MappingId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorRawDatas_MappingId",
                table: "SensorRawDatas",
                column: "MappingId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagTypeId",
                table: "Tags",
                column: "TagTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionData_MappingId",
                table: "TransactionData",
                column: "MappingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeLastDatas");

            migrationBuilder.DropTable(
                name: "SensorRawDatas");

            migrationBuilder.DropTable(
                name: "TransactionData");

            migrationBuilder.DropTable(
                name: "Mappings");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "TagTypes");
        }
    }
}
