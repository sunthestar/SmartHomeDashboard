using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartHomeDashboard.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Rooms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DeviceTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "LoginSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Password = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLoginTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginIp = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LoginCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastFailTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LockUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SceneName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TriggerType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TriggerCondition = table.Column<string>(type: "text", nullable: false),
                    Actions = table.Column<string>(type: "text", nullable: false),
                    ExecuteTime = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RepeatDays = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExecuteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastExecuteTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LogLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ActionDetail = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemLogs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TcpConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    FullDeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeoutCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TcpConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TcpConnections_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1073));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1075));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1078));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1079));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1082));

            migrationBuilder.UpdateData(
                table: "DeviceTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1083));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1111));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1115));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1117));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1124));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1126));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1129));

            migrationBuilder.InsertData(
                table: "LoginSettings",
                columns: new[] { "Id", "CreatedAt", "FailCount", "IsEnabled", "LastFailTime", "LastLoginIp", "LastLoginTime", "LockUntil", "LoginCount", "Password", "PasswordSalt", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(912), 0, true, null, "", null, null, 0, "123456", "", null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1035), null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1037), null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1040), null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1042), null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1046), null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1048), null });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1049), null });

            migrationBuilder.InsertData(
                table: "Scenes",
                columns: new[] { "Id", "Actions", "CreatedAt", "Description", "ExecuteCount", "ExecuteTime", "Icon", "IsActive", "LastExecuteTime", "RepeatDays", "SceneName", "TriggerCondition", "TriggerType", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "[{\"type\":\"light\",\"action\":\"off\"},{\"type\":\"ac\",\"action\":\"set_temperature\",\"value\":24}]", new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1147), "关闭所有灯光，调整空调温度", 0, "22:00", "fa-moon", true, null, "mon,tue,wed,thu,fri,sat,sun", "晚安模式", "{\"time\":\"22:00\"}", "time", null },
                    { 2, "[{\"type\":\"curtain\",\"action\":\"open\"},{\"type\":\"coffee\",\"action\":\"on\"}]", new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1151), "打开卧室窗帘，启动咖啡机", 0, "07:30", "fa-sun", true, null, "mon,tue,wed,thu,fri", "晨间唤醒", "{\"time\":\"07:30\"}", "time", null },
                    { 3, "[{\"type\":\"all\",\"action\":\"off\"},{\"type\":\"security\",\"action\":\"on\"}]", new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1155), "关闭所有设备，启动安防系统", 0, "", "fa-umbrella-beach", false, null, "", "离家布防", "{}", "manual", null },
                    { 4, "[{\"type\":\"light\",\"action\":\"dim\",\"value\":50},{\"type\":\"music\",\"action\":\"play\",\"playlist\":\"dinner\"}]", new DateTime(2026, 3, 20, 11, 5, 17, 95, DateTimeKind.Local).AddTicks(1157), "调暗灯光，播放音乐", 0, "18:00", "fa-pizza-slice", false, null, "mon,tue,wed,thu,fri", "晚餐模式", "{\"time\":\"18:00\"}", "time", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginSettings_Id",
                table: "LoginSettings",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_IsActive",
                table: "Scenes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_SceneName",
                table: "Scenes",
                column: "SceneName");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_TriggerType",
                table: "Scenes",
                column: "TriggerType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_DeviceId",
                table: "SystemLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_LogType",
                table: "SystemLogs",
                column: "LogType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Timestamp",
                table: "SystemLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TcpConnections_DeviceId",
                table: "TcpConnections",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TcpConnections_FullDeviceId",
                table: "TcpConnections",
                column: "FullDeviceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginSettings");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "TcpConnections");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DeviceTypes");

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3445));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3449));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3452));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3455));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3457));

            migrationBuilder.UpdateData(
                table: "Devices",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3460));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3314));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3325));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3327));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3328));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3329));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3330));

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 23, 13, 52, 972, DateTimeKind.Local).AddTicks(3332));
        }
    }
}
