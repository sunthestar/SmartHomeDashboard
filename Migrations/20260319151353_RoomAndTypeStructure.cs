using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartHomeDashboard.Migrations
{
    /// <inheritdoc />
    public partial class RoomAndTypeStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DeviceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OnlineCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DeviceNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FullDeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomIdentifier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TypeIdentifier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsOn = table.Column<bool>(type: "INTEGER", nullable: false),
                    StatusText = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Power = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PowerValue = table.Column<double>(type: "REAL", nullable: false),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Temperature = table.Column<double>(type: "decimal(5,2)", nullable: true),
                    Humidity = table.Column<int>(type: "INTEGER", nullable: true),
                    MotorSpeed = table.Column<int>(type: "INTEGER", nullable: true),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SwingVertical = table.Column<bool>(type: "INTEGER", nullable: true),
                    SwingHorizontal = table.Column<bool>(type: "INTEGER", nullable: true),
                    Light = table.Column<bool>(type: "INTEGER", nullable: true),
                    Quiet = table.Column<bool>(type: "INTEGER", nullable: true),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_DeviceTypes_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "DeviceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DeviceTypes",
                columns: new[] { "Id", "Description", "Icon", "TypeId", "TypeName" },
                values: new object[,]
                {
                    { 1, "智能空调", "fa-wind", "ac", "空调" },
                    { 2, "智能灯泡", "fa-lightbulb", "light", "灯光" },
                    { 3, "智能门锁", "fa-lock", "lock", "门锁" },
                    { 4, "网络摄像头", "fa-camera", "camera", "摄像头" },
                    { 5, "智能风扇", "fa-fan", "fan", "风扇" },
                    { 6, "温度传感器", "fa-thermometer-half", "temp-sensor", "温度传感器" },
                    { 7, "湿度传感器", "fa-tint", "humidity-sensor", "湿度传感器" },
                    { 8, "电机设备", "fa-cogs", "motor", "电机" }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "CreatedAt", "Description", "DeviceCount", "OnlineCount", "RoomId", "RoomName" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3314), "主要活动区域", 0, 0, "living", "客厅" },
                    { 2, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3325), "主人卧室", 0, 0, "master-bedroom", "主卧" },
                    { 3, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3327), "次卧/客房", 0, 0, "second-bedroom", "次卧" },
                    { 4, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3328), "烹饪区域", 0, 0, "kitchen", "厨房" },
                    { 5, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3329), "洗浴区域", 0, 0, "bathroom", "浴室" },
                    { 6, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3330), "用餐区域", 0, 0, "dining", "餐厅" },
                    { 7, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3332), "玄关/入口", 0, 0, "entrance", "入口" }
                });

            migrationBuilder.InsertData(
                table: "Devices",
                columns: new[] { "Id", "CreatedAt", "Detail", "DeviceNumber", "DeviceTypeId", "Direction", "FullDeviceId", "Humidity", "Icon", "IsOn", "Light", "Mode", "MotorSpeed", "Name", "Power", "PowerValue", "Progress", "ProgressColor", "Quiet", "RoomId", "RoomIdentifier", "StatusText", "SwingHorizontal", "SwingVertical", "Temperature", "TypeIdentifier", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3445), "空调 · 等待连接", "001", 1, null, "living-ac-001", null, "fa-wind", false, false, "cool", null, "客厅空调", "0W", 0.0, 0, "#a0a0a0", false, 1, "living", "离线", false, false, 24.0, "ac", null },
                    { 2, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3449), "灯光 · 等待连接", "001", 2, null, "living-light-001", null, "fa-lightbulb", false, null, null, null, "客厅灯光", "0W", 0.0, 0, "#a0a0a0", null, 1, "living", "离线", null, null, null, "light", null },
                    { 3, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3452), "门锁 · 等待连接", "001", 3, null, "entrance-lock-001", 100, "fa-lock", false, null, null, null, "入口门锁", "0W", 0.0, 0, "#a0a0a0", null, 7, "entrance", "离线", null, null, null, "lock", null },
                    { 4, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3455), "温度传感器 · 等待连接", "001", 6, null, "kitchen-temp-sensor-001", null, "fa-thermometer-half", false, null, null, null, "厨房温度传感器", "0W", 0.0, 0, "#a0a0a0", null, 4, "kitchen", "离线", null, null, 22.5, "temp-sensor", null },
                    { 5, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3457), "湿度传感器 · 等待连接", "001", 7, null, "bathroom-humidity-sensor-001", 65, "fa-tint", false, null, null, null, "浴室湿度传感器", "0W", 0.0, 0, "#a0a0a0", null, 5, "bathroom", "离线", null, null, null, "humidity-sensor", null },
                    { 6, new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3460), "风扇 · 等待连接", "001", 5, null, "master-bedroom-fan-001", null, "fa-fan", false, null, null, 3, "卧室风扇", "0W", 0.0, 0, "#a0a0a0", null, 2, "master-bedroom", "离线", null, null, null, "fan", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceTypeId",
                table: "Devices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_FullDeviceId",
                table: "Devices",
                column: "FullDeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_RoomId_DeviceTypeId_DeviceNumber",
                table: "Devices",
                columns: new[] { "RoomId", "DeviceTypeId", "DeviceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTypes_TypeId",
                table: "DeviceTypes",
                column: "TypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomId",
                table: "Rooms",
                column: "RoomId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "DeviceTypes");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
