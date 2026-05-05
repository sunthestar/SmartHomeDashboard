# 将解决方案代码整理为单个 Markdown 文件

下面给出一个 PowerShell 命令，可在工作区根目录运行，将当前解决方案中的源码文件合并为一个名为 ALL_CODE.md 的 Markdown 文件，且为每个文件添加标题和代码块。

运行步骤（在工作区根目录 D:\21799\现在\毕业设计\AI时代下智能家居系统设计与实现\AI时代下智能家居系统\SmartHomeDashboard\SmartHomeDashboard\ 中运行 PowerShell）：

1. 打开 PowerShell（开发者偏好）。
2. 运行以下命令：

```powershell
Get-ChildItem -Recurse -File -Include *.cs,*.cshtml,*.cshtml.cs,*.csproj,*.json |
    Sort-Object FullName |
    ForEach-Object {
        $rel = $_.FullName.Substring((Get-Location).Path.Length).TrimStart('\\')
        $lang = if ($_.Extension -eq '.cs' -or $_.Extension -eq '.csproj') { 'csharp' }
                elseif ($_.Extension -eq '.cshtml') { 'html' }
                elseif ($_.Extension -eq '.json') { 'json' }
                else { '' }
        "## $rel`n`n```$lang`n$([System.IO.File]::ReadAllText($_.FullName))`n```" 
    } | Out-File -FilePath ALL_CODE.md -Encoding utf8
```

说明：
- 该命令会递归查找常见源码文件并按路径排序输出到 ALL_CODE.md，每个文件前添加二级标题和语言标注的代码块。
- 若需要包含其他扩展名，修改 -Include 列表。

已列出的工程文件（部分）：

- Models/SystemLog.cs
- Services/LoginService.cs
- Migrations/20260319151353_RoomAndTypeStructure.Designer.cs
- Pages/Test.cshtml.cs
- Services/RoomService.cs
- Migrations/AppDbContextModelSnapshot.cs
- Migrations/20260321083358_InitialCreate.Designer.cs
- Pages/Login.cshtml.cs
- Migrations/20260319151353_RoomAndTypeStructure.cs
- Controllers/TcpController.cs
- Pages/Error.cshtml.cs
- Services/AIAssistantService.cs
- Services/TcpDeviceService.cs
- Middleware/LoginCheckMiddleware.cs
- Data/AppDbContext.cs
- Hubs/DeviceHub.cs
- Services/TcpConnectionService.cs
- Services/SceneService.cs
- Services/TcpServerService.cs
- Controllers/ApiController.cs
- Models/DeviceModels.cs
- Migrations/20260321083358_InitialCreate.cs
- Pages/Privacy.cshtml.cs
- Controllers/AIAssistantController.cs
- Pages/Index.cshtml.cs
- Models/TcpMessage.cs
- Services/SystemLogService.cs
- Services/DeviceDataService.cs
- Program.cs

如果你希望我现在直接在工作区生成包含所有源码内容的 ALL_CODE.md 文件，请确认，我会读取所有文件并创建该文件（注意：文件可能很大）。