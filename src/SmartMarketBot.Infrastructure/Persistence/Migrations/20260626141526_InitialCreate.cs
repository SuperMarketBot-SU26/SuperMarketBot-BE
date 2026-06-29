using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCOUNT",
                columns: table => new
                {
                    AccountID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    OtpExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OtpType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTokenRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCOUNT", x => x.AccountID);
                });

            migrationBuilder.CreateTable(
                name: "AD_PACKAGE",
                columns: table => new
                {
                    PackageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PricePackage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PriceRoute = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    BasePriceClick = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    AdScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_PACKAGE", x => x.PackageID);
                });

            migrationBuilder.CreateTable(
                name: "BRAND",
                columns: table => new
                {
                    BrandID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Wallet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BRAND", x => x.BrandID);
                });

            migrationBuilder.CreateTable(
                name: "CATEGORY",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORY", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "FLOOR",
                columns: table => new
                {
                    FloorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FLOOR", x => x.FloorID);
                });

            migrationBuilder.CreateTable(
                name: "HEALTH_TAG",
                columns: table => new
                {
                    HealthTagID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TagType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "diet")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HEALTH_TAG", x => x.HealthTagID);
                });

            migrationBuilder.CreateTable(
                name: "MEAL_SUGGESTION",
                columns: table => new
                {
                    MealSuggestionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YieldPortions = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Calories = table.Column<int>(type: "int", nullable: true),
                    healthy_score = table.Column<int>(type: "int", nullable: true),
                    alternative_suggestion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEAL_SUGGESTION", x => x.MealSuggestionID);
                });

            migrationBuilder.CreateTable(
                name: "ROBOT",
                columns: table => new
                {
                    RobotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RobotCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BatteryPct = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "idle"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROBOT", x => x.RobotID);
                });

            migrationBuilder.CreateTable(
                name: "MEMBER",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FacePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FaceVector = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpendingLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBER", x => x.MemberID);
                    table.ForeignKey(
                        name: "FK_MEMBER_ACCOUNT_AccountID",
                        column: x => x.AccountID,
                        principalTable: "ACCOUNT",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SUBCATEGORY",
                columns: table => new
                {
                    SubcategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    SubcategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUBCATEGORY", x => x.SubcategoryID);
                    table.ForeignKey(
                        name: "FK_SUBCATEGORY_CATEGORY_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "CATEGORY",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MAP",
                columns: table => new
                {
                    MapID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorID = table.Column<int>(type: "int", nullable: false),
                    MapName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MapData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FloorplanImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAP", x => x.MapID);
                    table.ForeignKey(
                        name: "FK_MAP_FLOOR_FloorID",
                        column: x => x.FloorID,
                        principalTable: "FLOOR",
                        principalColumn: "FloorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZONE",
                columns: table => new
                {
                    ZoneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorID = table.Column<int>(type: "int", nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZONE", x => x.ZoneID);
                    table.ForeignKey(
                        name: "FK_ZONE_FLOOR_FloorID",
                        column: x => x.FloorID,
                        principalTable: "FLOOR",
                        principalColumn: "FloorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROBOT_LOG",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotID = table.Column<int>(type: "int", nullable: true),
                    battery = table.Column<int>(type: "int", nullable: true),
                    location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    XCoord = table.Column<double>(type: "float", nullable: true),
                    YCoord = table.Column<double>(type: "float", nullable: true),
                    HeadingRad = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROBOT_LOG", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_ROBOT_LOG_ROBOT_RobotID",
                        column: x => x.RobotID,
                        principalTable: "ROBOT",
                        principalColumn: "RobotID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CART",
                columns: table => new
                {
                    CartID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CART", x => x.CartID);
                    table.ForeignKey(
                        name: "FK_CART_MEMBER_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MEMBER",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INVOICE_HISTORY",
                columns: table => new
                {
                    InvoiceHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICE_HISTORY", x => x.InvoiceHistoryID);
                    table.ForeignKey(
                        name: "FK_INVOICE_HISTORY_MEMBER_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MEMBER",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEMBER_NOTIFICATION",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    NotifType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBER_NOTIFICATION", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_MEMBER_NOTIFICATION_MEMBER_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MEMBER",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEMBERHEALTH_PREFERENCE",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    HealthTagID = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBERHEALTH_PREFERENCE", x => new { x.MemberID, x.HealthTagID });
                    table.ForeignKey(
                        name: "FK_MEMBERHEALTH_PREFERENCE_HEALTH_TAG_HealthTagID",
                        column: x => x.HealthTagID,
                        principalTable: "HEALTH_TAG",
                        principalColumn: "HealthTagID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MEMBERHEALTH_PREFERENCE_MEMBER_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MEMBER",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEMBERSHIP",
                columns: table => new
                {
                    MembershipID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    TierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBERSHIP", x => x.MembershipID);
                    table.ForeignKey(
                        name: "FK_MEMBERSHIP_MEMBER_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MEMBER",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCT_TYPE",
                columns: table => new
                {
                    ProductTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubcategoryID = table.Column<int>(type: "int", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCT_TYPE", x => x.ProductTypeID);
                    table.ForeignKey(
                        name: "FK_PRODUCT_TYPE_SUBCATEGORY_SubcategoryID",
                        column: x => x.SubcategoryID,
                        principalTable: "SUBCATEGORY",
                        principalColumn: "SubcategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NAVIGATION_NODE",
                columns: table => new
                {
                    NodeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapID = table.Column<int>(type: "int", nullable: false),
                    NodeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    XCoord = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    YCoord = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    NodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NAVIGATION_NODE", x => x.NodeID);
                    table.ForeignKey(
                        name: "FK_NAVIGATION_NODE_MAP_MapID",
                        column: x => x.MapID,
                        principalTable: "MAP",
                        principalColumn: "MapID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROBOT_ROUTE",
                columns: table => new
                {
                    RobotRouteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotID = table.Column<int>(type: "int", nullable: false),
                    MapID = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROBOT_ROUTE", x => x.RobotRouteID);
                    table.ForeignKey(
                        name: "FK_ROBOT_ROUTE_MAP_MapID",
                        column: x => x.MapID,
                        principalTable: "MAP",
                        principalColumn: "MapID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROBOT_ROUTE_ROBOT_RobotID",
                        column: x => x.RobotID,
                        principalTable: "ROBOT",
                        principalColumn: "RobotID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AISLE",
                columns: table => new
                {
                    AisleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneID = table.Column<int>(type: "int", nullable: false),
                    AisleCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AisleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISLE", x => x.AisleID);
                    table.ForeignKey(
                        name: "FK_AISLE_ZONE_ZoneID",
                        column: x => x.ZoneID,
                        principalTable: "ZONE",
                        principalColumn: "ZoneID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROBOT_ZONE",
                columns: table => new
                {
                    RobotZoneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotID = table.Column<int>(type: "int", nullable: false),
                    ZoneID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROBOT_ZONE", x => x.RobotZoneID);
                    table.ForeignKey(
                        name: "FK_ROBOT_ZONE_ROBOT_RobotID",
                        column: x => x.RobotID,
                        principalTable: "ROBOT",
                        principalColumn: "RobotID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROBOT_ZONE_ZONE_ZoneID",
                        column: x => x.ZoneID,
                        principalTable: "ZONE",
                        principalColumn: "ZoneID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCT",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductTypeID = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PromotionPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpiredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WeightOrVolume = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubstituteProductID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCT", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_PRODUCT_PRODUCT_SubstituteProductID",
                        column: x => x.SubstituteProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_PRODUCT_PRODUCT_TYPE_ProductTypeID",
                        column: x => x.ProductTypeID,
                        principalTable: "PRODUCT_TYPE",
                        principalColumn: "ProductTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NAVIGATION_EDGE",
                columns: table => new
                {
                    EdgeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromNodeID = table.Column<int>(type: "int", nullable: false),
                    ToNodeID = table.Column<int>(type: "int", nullable: false),
                    Distance = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    IsBidirectional = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NAVIGATION_EDGE", x => x.EdgeID);
                    table.ForeignKey(
                        name: "FK_NAVIGATION_EDGE_NAVIGATION_NODE_FromNodeID",
                        column: x => x.FromNodeID,
                        principalTable: "NAVIGATION_NODE",
                        principalColumn: "NodeID");
                    table.ForeignKey(
                        name: "FK_NAVIGATION_EDGE_NAVIGATION_NODE_ToNodeID",
                        column: x => x.ToNodeID,
                        principalTable: "NAVIGATION_NODE",
                        principalColumn: "NodeID");
                });

            migrationBuilder.CreateTable(
                name: "ROUTE_ASSIGNMENT",
                columns: table => new
                {
                    RouteAssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotID = table.Column<int>(type: "int", nullable: false),
                    RobotRouteID = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROUTE_ASSIGNMENT", x => x.RouteAssignmentID);
                    table.ForeignKey(
                        name: "FK_ROUTE_ASSIGNMENT_ROBOT_ROUTE_RobotRouteID",
                        column: x => x.RobotRouteID,
                        principalTable: "ROBOT_ROUTE",
                        principalColumn: "RobotRouteID");
                    table.ForeignKey(
                        name: "FK_ROUTE_ASSIGNMENT_ROBOT_RobotID",
                        column: x => x.RobotID,
                        principalTable: "ROBOT",
                        principalColumn: "RobotID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROUTE_NODE_MAPPING",
                columns: table => new
                {
                    RouteNodeMappingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotRouteID = table.Column<int>(type: "int", nullable: false),
                    NodeID = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROUTE_NODE_MAPPING", x => x.RouteNodeMappingID);
                    table.ForeignKey(
                        name: "FK_ROUTE_NODE_MAPPING_NAVIGATION_NODE_NodeID",
                        column: x => x.NodeID,
                        principalTable: "NAVIGATION_NODE",
                        principalColumn: "NodeID");
                    table.ForeignKey(
                        name: "FK_ROUTE_NODE_MAPPING_ROBOT_ROUTE_RobotRouteID",
                        column: x => x.RobotRouteID,
                        principalTable: "ROBOT_ROUTE",
                        principalColumn: "RobotRouteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AISLE_NODE",
                columns: table => new
                {
                    AisleNodeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AisleID = table.Column<int>(type: "int", nullable: false),
                    NodeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISLE_NODE", x => x.AisleNodeID);
                    table.ForeignKey(
                        name: "FK_AISLE_NODE_AISLE_AisleID",
                        column: x => x.AisleID,
                        principalTable: "AISLE",
                        principalColumn: "AisleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AISLE_NODE_NAVIGATION_NODE_NodeID",
                        column: x => x.NodeID,
                        principalTable: "NAVIGATION_NODE",
                        principalColumn: "NodeID");
                });

            migrationBuilder.CreateTable(
                name: "SHELF",
                columns: table => new
                {
                    ShelfID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AisleID = table.Column<int>(type: "int", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SHELF", x => x.ShelfID);
                    table.ForeignKey(
                        name: "FK_SHELF_AISLE_AisleID",
                        column: x => x.AisleID,
                        principalTable: "AISLE",
                        principalColumn: "AisleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AD_CAMPAIGN",
                columns: table => new
                {
                    AdCampaignID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageID = table.Column<int>(type: "int", nullable: false),
                    BrandID = table.Column<int>(type: "int", nullable: false),
                    RobotZoneID = table.Column<int>(type: "int", nullable: true),
                    CampaignName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_CAMPAIGN", x => x.AdCampaignID);
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_AD_PACKAGE_PackageID",
                        column: x => x.PackageID,
                        principalTable: "AD_PACKAGE",
                        principalColumn: "PackageID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_BRAND_BrandID",
                        column: x => x.BrandID,
                        principalTable: "BRAND",
                        principalColumn: "BrandID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_ROBOT_ZONE_RobotZoneID",
                        column: x => x.RobotZoneID,
                        principalTable: "ROBOT_ZONE",
                        principalColumn: "RobotZoneID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CART_ITEM",
                columns: table => new
                {
                    CartItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CART_ITEM", x => x.CartItemID);
                    table.ForeignKey(
                        name: "FK_CART_ITEM_CART_CartID",
                        column: x => x.CartID,
                        principalTable: "CART",
                        principalColumn: "CartID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CART_ITEM_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "INVOICE_HISTORY_ITEM",
                columns: table => new
                {
                    InvoiceHistoryItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceHistoryID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICE_HISTORY_ITEM", x => x.InvoiceHistoryItemID);
                    table.ForeignKey(
                        name: "FK_INVOICE_HISTORY_ITEM_INVOICE_HISTORY_InvoiceHistoryID",
                        column: x => x.InvoiceHistoryID,
                        principalTable: "INVOICE_HISTORY",
                        principalColumn: "InvoiceHistoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVOICE_HISTORY_ITEM_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEAL_ITEM",
                columns: table => new
                {
                    MealSuggestionID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 1m),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEAL_ITEM", x => new { x.MealSuggestionID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_MEAL_ITEM_MEAL_SUGGESTION_MealSuggestionID",
                        column: x => x.MealSuggestionID,
                        principalTable: "MEAL_SUGGESTION",
                        principalColumn: "MealSuggestionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MEAL_ITEM_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCT_HEALTHTAG",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    HealthTagID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCT_HEALTHTAG", x => new { x.ProductID, x.HealthTagID });
                    table.ForeignKey(
                        name: "FK_PRODUCT_HEALTHTAG_HEALTH_TAG_HealthTagID",
                        column: x => x.HealthTagID,
                        principalTable: "HEALTH_TAG",
                        principalColumn: "HealthTagID");
                    table.ForeignKey(
                        name: "FK_PRODUCT_HEALTHTAG_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SEMANTIC_OBJECT",
                columns: table => new
                {
                    ObjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapID = table.Column<int>(type: "int", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    XMin = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    YMin = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    XMax = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    YMax = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEMANTIC_OBJECT", x => x.ObjectID);
                    table.ForeignKey(
                        name: "FK_SEMANTIC_OBJECT_MAP_MapID",
                        column: x => x.MapID,
                        principalTable: "MAP",
                        principalColumn: "MapID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SEMANTIC_OBJECT_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AISLE_SCAN",
                columns: table => new
                {
                    ScanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AisleID = table.Column<int>(type: "int", nullable: false),
                    AisleNodeID = table.Column<int>(type: "int", nullable: true),
                    RobotID = table.Column<int>(type: "int", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    EmptyPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    DensityPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 100m),
                    NeedsRestock = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISLE_SCAN", x => x.ScanID);
                    table.ForeignKey(
                        name: "FK_AISLE_SCAN_AISLE_AisleID",
                        column: x => x.AisleID,
                        principalTable: "AISLE",
                        principalColumn: "AisleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AISLE_SCAN_AISLE_NODE_AisleNodeID",
                        column: x => x.AisleNodeID,
                        principalTable: "AISLE_NODE",
                        principalColumn: "AisleNodeID");
                    table.ForeignKey(
                        name: "FK_AISLE_SCAN_ROBOT_RobotID",
                        column: x => x.RobotID,
                        principalTable: "ROBOT",
                        principalColumn: "RobotID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SLOT",
                columns: table => new
                {
                    SlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShelfID = table.Column<int>(type: "int", nullable: false),
                    SlotCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLOT", x => x.SlotID);
                    table.ForeignKey(
                        name: "FK_SLOT_SHELF_ShelfID",
                        column: x => x.ShelfID,
                        principalTable: "SHELF",
                        principalColumn: "ShelfID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AD_RESOURCE",
                columns: table => new
                {
                    ResourceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdCampaignID = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResourceURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_RESOURCE", x => x.ResourceID);
                    table.ForeignKey(
                        name: "FK_AD_RESOURCE_AD_CAMPAIGN_AdCampaignID",
                        column: x => x.AdCampaignID,
                        principalTable: "AD_CAMPAIGN",
                        principalColumn: "AdCampaignID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SPONSORED_PRODUCT",
                columns: table => new
                {
                    SponsoredID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdCampaignID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SPONSORED_PRODUCT", x => x.SponsoredID);
                    table.ForeignKey(
                        name: "FK_SPONSORED_PRODUCT_AD_CAMPAIGN_AdCampaignID",
                        column: x => x.AdCampaignID,
                        principalTable: "AD_CAMPAIGN",
                        principalColumn: "AdCampaignID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SPONSORED_PRODUCT_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCT_SLOT",
                columns: table => new
                {
                    ProductsSlotID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlotID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCT_SLOT", x => x.ProductsSlotID);
                    table.ForeignKey(
                        name: "FK_PRODUCT_SLOT_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PRODUCT_SLOT_SLOT_SlotID",
                        column: x => x.SlotID,
                        principalTable: "SLOT",
                        principalColumn: "SlotID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AD_CAMPAIGN_LOG",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdCampaignID = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChargedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "DATEADD(hour, 7, GETUTCDATE())"),
                    SponsoredID = table.Column<int>(type: "int", nullable: true),
                    ProductID = table.Column<int>(type: "int", nullable: true),
                    RobotID = table.Column<int>(type: "int", nullable: true),
                    RobotZoneID = table.Column<int>(type: "int", nullable: true),
                    ZoneID = table.Column<int>(type: "int", nullable: true),
                    SlotID = table.Column<int>(type: "int", nullable: true),
                    MemberID = table.Column<int>(type: "int", nullable: true),
                    SessionID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    XCoord = table.Column<int>(type: "int", nullable: true),
                    YCoord = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_CAMPAIGN_LOG", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_AD_CAMPAIGN_AdCampaignID",
                        column: x => x.AdCampaignID,
                        principalTable: "AD_CAMPAIGN",
                        principalColumn: "AdCampaignID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_MEMBER_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MEMBER",
                        principalColumn: "MemberID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_PRODUCT_ProductID",
                        column: x => x.ProductID,
                        principalTable: "PRODUCT",
                        principalColumn: "ProductID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_ROBOT_RobotID",
                        column: x => x.RobotID,
                        principalTable: "ROBOT",
                        principalColumn: "RobotID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_ROBOT_ZONE_RobotZoneID",
                        column: x => x.RobotZoneID,
                        principalTable: "ROBOT_ZONE",
                        principalColumn: "RobotZoneID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_SLOT_SlotID",
                        column: x => x.SlotID,
                        principalTable: "SLOT",
                        principalColumn: "SlotID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_SPONSORED_PRODUCT_SponsoredID",
                        column: x => x.SponsoredID,
                        principalTable: "SPONSORED_PRODUCT",
                        principalColumn: "SponsoredID");
                    table.ForeignKey(
                        name: "FK_AD_CAMPAIGN_LOG_ZONE_ZoneID",
                        column: x => x.ZoneID,
                        principalTable: "ZONE",
                        principalColumn: "ZoneID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_Email",
                table: "ACCOUNT",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_Username",
                table: "ACCOUNT",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_BrandID",
                table: "AD_CAMPAIGN",
                column: "BrandID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_PackageID",
                table: "AD_CAMPAIGN",
                column: "PackageID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_RobotZoneID",
                table: "AD_CAMPAIGN",
                column: "RobotZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_AdCampaignID",
                table: "AD_CAMPAIGN_LOG",
                column: "AdCampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_MemberID",
                table: "AD_CAMPAIGN_LOG",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_ProductID",
                table: "AD_CAMPAIGN_LOG",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_RobotID",
                table: "AD_CAMPAIGN_LOG",
                column: "RobotID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_RobotZoneID",
                table: "AD_CAMPAIGN_LOG",
                column: "RobotZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_SlotID",
                table: "AD_CAMPAIGN_LOG",
                column: "SlotID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_SponsoredID",
                table: "AD_CAMPAIGN_LOG",
                column: "SponsoredID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_CAMPAIGN_LOG_ZoneID",
                table: "AD_CAMPAIGN_LOG",
                column: "ZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_AD_RESOURCE_AdCampaignID",
                table: "AD_RESOURCE",
                column: "AdCampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_ZoneID",
                table: "AISLE",
                column: "ZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_NODE_AisleID",
                table: "AISLE_NODE",
                column: "AisleID");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_NODE_NodeID",
                table: "AISLE_NODE",
                column: "NodeID");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_SCAN_AisleID",
                table: "AISLE_SCAN",
                column: "AisleID");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_SCAN_AisleNodeID",
                table: "AISLE_SCAN",
                column: "AisleNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_SCAN_RobotID",
                table: "AISLE_SCAN",
                column: "RobotID");

            migrationBuilder.CreateIndex(
                name: "IX_CART_MemberID",
                table: "CART",
                column: "MemberID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CART_ITEM_CartID",
                table: "CART_ITEM",
                column: "CartID");

            migrationBuilder.CreateIndex(
                name: "IX_CART_ITEM_ProductID",
                table: "CART_ITEM",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_HISTORY_MemberID",
                table: "INVOICE_HISTORY",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_HISTORY_ITEM_InvoiceHistoryID",
                table: "INVOICE_HISTORY_ITEM",
                column: "InvoiceHistoryID");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_HISTORY_ITEM_ProductID",
                table: "INVOICE_HISTORY_ITEM",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_MAP_FloorID",
                table: "MAP",
                column: "FloorID");

            migrationBuilder.CreateIndex(
                name: "IX_MEAL_ITEM_ProductID",
                table: "MEAL_ITEM",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_MEMBER_AccountID",
                table: "MEMBER",
                column: "AccountID",
                unique: true,
                filter: "[AccountID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MN_CreatedAt",
                table: "MEMBER_NOTIFICATION",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_MN_MemberID_IsRead",
                table: "MEMBER_NOTIFICATION",
                columns: new[] { "MemberID", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_MEMBERHEALTH_PREFERENCE_HealthTagID",
                table: "MEMBERHEALTH_PREFERENCE",
                column: "HealthTagID");

            migrationBuilder.CreateIndex(
                name: "IX_MEMBERSHIP_MemberID",
                table: "MEMBERSHIP",
                column: "MemberID");

            migrationBuilder.CreateIndex(
                name: "IX_NAVIGATION_EDGE_FromNodeID",
                table: "NAVIGATION_EDGE",
                column: "FromNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_NAVIGATION_EDGE_ToNodeID",
                table: "NAVIGATION_EDGE",
                column: "ToNodeID");

            migrationBuilder.CreateIndex(
                name: "IX_NAVIGATION_NODE_MapID",
                table: "NAVIGATION_NODE",
                column: "MapID");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_ProductTypeID",
                table: "PRODUCT",
                column: "ProductTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_SubstituteProductID",
                table: "PRODUCT",
                column: "SubstituteProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_HEALTHTAG_HealthTagID",
                table: "PRODUCT_HEALTHTAG",
                column: "HealthTagID");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_SLOT_ProductID",
                table: "PRODUCT_SLOT",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_SLOT_slot_product",
                table: "PRODUCT_SLOT",
                columns: new[] { "SlotID", "ProductID" });

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCT_TYPE_SubcategoryID",
                table: "PRODUCT_TYPE",
                column: "SubcategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_RobotCode",
                table: "ROBOT",
                column: "RobotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_LOG_robot_timestamp",
                table: "ROBOT_LOG",
                columns: new[] { "RobotID", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_ROUTE_MapID",
                table: "ROBOT_ROUTE",
                column: "MapID");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_ROUTE_RobotID",
                table: "ROBOT_ROUTE",
                column: "RobotID");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_ZONE_RobotID",
                table: "ROBOT_ZONE",
                column: "RobotID");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_ZONE_ZoneID",
                table: "ROBOT_ZONE",
                column: "ZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTE_ASSIGNMENT_RobotID",
                table: "ROUTE_ASSIGNMENT",
                column: "RobotID");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTE_ASSIGNMENT_RobotRouteID",
                table: "ROUTE_ASSIGNMENT",
                column: "RobotRouteID");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTE_NODE_MAPPING_NodeID",
                table: "ROUTE_NODE_MAPPING",
                column: "NodeID");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTE_NODE_MAPPING_RobotRouteID",
                table: "ROUTE_NODE_MAPPING",
                column: "RobotRouteID");

            migrationBuilder.CreateIndex(
                name: "IX_SEMANTIC_OBJECT_MapID",
                table: "SEMANTIC_OBJECT",
                column: "MapID");

            migrationBuilder.CreateIndex(
                name: "IX_SEMANTIC_OBJECT_ProductID",
                table: "SEMANTIC_OBJECT",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_SHELF_AisleID",
                table: "SHELF",
                column: "AisleID");

            migrationBuilder.CreateIndex(
                name: "IX_SLOT_ShelfID",
                table: "SLOT",
                column: "ShelfID");

            migrationBuilder.CreateIndex(
                name: "IX_SPONSORED_PRODUCT_AdCampaignID",
                table: "SPONSORED_PRODUCT",
                column: "AdCampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_SPONSORED_PRODUCT_ProductID",
                table: "SPONSORED_PRODUCT",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_SUBCATEGORY_CategoryID",
                table: "SUBCATEGORY",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ZONE_FloorID",
                table: "ZONE",
                column: "FloorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AD_CAMPAIGN_LOG");

            migrationBuilder.DropTable(
                name: "AD_RESOURCE");

            migrationBuilder.DropTable(
                name: "AISLE_SCAN");

            migrationBuilder.DropTable(
                name: "CART_ITEM");

            migrationBuilder.DropTable(
                name: "INVOICE_HISTORY_ITEM");

            migrationBuilder.DropTable(
                name: "MEAL_ITEM");

            migrationBuilder.DropTable(
                name: "MEMBER_NOTIFICATION");

            migrationBuilder.DropTable(
                name: "MEMBERHEALTH_PREFERENCE");

            migrationBuilder.DropTable(
                name: "MEMBERSHIP");

            migrationBuilder.DropTable(
                name: "NAVIGATION_EDGE");

            migrationBuilder.DropTable(
                name: "PRODUCT_HEALTHTAG");

            migrationBuilder.DropTable(
                name: "PRODUCT_SLOT");

            migrationBuilder.DropTable(
                name: "ROBOT_LOG");

            migrationBuilder.DropTable(
                name: "ROUTE_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "ROUTE_NODE_MAPPING");

            migrationBuilder.DropTable(
                name: "SEMANTIC_OBJECT");

            migrationBuilder.DropTable(
                name: "SPONSORED_PRODUCT");

            migrationBuilder.DropTable(
                name: "AISLE_NODE");

            migrationBuilder.DropTable(
                name: "CART");

            migrationBuilder.DropTable(
                name: "INVOICE_HISTORY");

            migrationBuilder.DropTable(
                name: "MEAL_SUGGESTION");

            migrationBuilder.DropTable(
                name: "HEALTH_TAG");

            migrationBuilder.DropTable(
                name: "SLOT");

            migrationBuilder.DropTable(
                name: "ROBOT_ROUTE");

            migrationBuilder.DropTable(
                name: "AD_CAMPAIGN");

            migrationBuilder.DropTable(
                name: "PRODUCT");

            migrationBuilder.DropTable(
                name: "NAVIGATION_NODE");

            migrationBuilder.DropTable(
                name: "MEMBER");

            migrationBuilder.DropTable(
                name: "SHELF");

            migrationBuilder.DropTable(
                name: "AD_PACKAGE");

            migrationBuilder.DropTable(
                name: "BRAND");

            migrationBuilder.DropTable(
                name: "ROBOT_ZONE");

            migrationBuilder.DropTable(
                name: "PRODUCT_TYPE");

            migrationBuilder.DropTable(
                name: "MAP");

            migrationBuilder.DropTable(
                name: "ACCOUNT");

            migrationBuilder.DropTable(
                name: "AISLE");

            migrationBuilder.DropTable(
                name: "ROBOT");

            migrationBuilder.DropTable(
                name: "SUBCATEGORY");

            migrationBuilder.DropTable(
                name: "ZONE");

            migrationBuilder.DropTable(
                name: "CATEGORY");

            migrationBuilder.DropTable(
                name: "FLOOR");
        }
    }
}
