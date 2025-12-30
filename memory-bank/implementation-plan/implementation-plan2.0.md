# AI 文档差异助手 - V1.1 实施计划

> 基于 `memory-bank/prd/prd2.0.md` (V1.1) 和 `memory-bank/tech-stack.md`
> 技术栈: C# + .NET 10 + WPF (AOT 编译)
> 状态: V1.0 已在 progress1.0.md 中完成，**本计划仅包含 V1.1 新增/变更内容**

---

## V1.1 变更摘要

| 特性 | V1.0 | V1.1 |
|------|------|------|
| 文件格式 | 仅纯文本 | 新增 .docx 和 .pdf |
| 通知方式 | MessageBox | Windows 原生 Toast |
| 预处理 | 无 | 识别后缀并提取纯文本 |
| 异常处理 | 基础 | PDF 加密/扫描件识别 |

---

## 阶段一：V1.1 准备工作

### 步骤 0：清理 V1.0 富文本实现

**指令：**
1. 检查 `src/AI.DiffAssistant.Core/File/` 是否存在 `RichTextProcessor.cs`
2. 如果存在，**删除该文件**
3. 检查 `FileProcessor.cs` 中是否有对 `RichTextProcessor` 的引用，如有则移除

**验证：**
- `dotnet build` 无编译错误

---

### 步骤 1：添加 V1.1 NuGet 依赖

**指令：**
在 `AI.DiffAssistant.Core.csproj` 添加：
```xml
<PackageReference Include="DocumentFormat.OpenXml" Version="3.0.2" />
<PackageReference Include="PdfPig" Version="0.1.8" />
<PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
```

**验证：**
- `dotnet restore` 成功
- `dotnet build` 无错误

---

### 步骤 2：CLI 改为 Windows Application

**指令：**
1. 修改 `AI.DiffAssistant.Cli.csproj`：
   ```xml
   <OutputType>WinExe</OutputType>
   <SubApplication>true</SubApplication>
   ```
2. 添加 `app.manifest` 文件

**验证：**
- 发布后启动无 DOS 窗口
- Toast 通知正常工作

---

## 阶段二：共享模型新增

### 步骤 3：定义文件解析结果模型

**指令：**
在 `AI.DiffAssistant.Shared/Models/` 创建：
1. `ParseResult.cs`:
   ```csharp
   public class ParseResult
   {
       public string Content { get; set; }
       public string SourceFileType { get; set; }  // 如 ".pdf"
       public bool IsSuccess { get; set; }
       public string ErrorMessage { get; set; }
       public int CharCount { get; set; }
   }
   ```
2. `FileParseResult.cs`: 封装两个文件的 ParseResult

**验证：**
- 单元测试通过

---

## 阶段三：富文本解析器

### 步骤 4：实现 Word (.docx) 解析器

**指令：**
创建 `AI.DiffAssistant.Core/Parser/DocxParser.cs`:
```csharp
public class DocxParser : IFileParser
{
    public bool CanParse(string ext) => ext.Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public ParseResult Parse(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var text = doc.MainDocumentPart.Document.Body.InnerText;
        // 遍历 Paragraphs 拼接换行符
        return new ParseResult {
            Content = text,
            SourceFileType = ".docx",
            IsSuccess = true,
            CharCount = text.Length
        };
    }
}
```
- 忽略：页眉、页脚、批注、图片
- 表格：提取文字，保留单元格分隔符

**验证：**
- 测试 .docx 文件解析正确
- 表格文字被提取

---

### 步骤 5：实现 PDF (.pdf) 解析器

**指令：**
创建 `AI.DiffAssistant.Core/Parser/PdfParser.cs`:
```csharp
public class PdfParser : IFileParser
{
    public bool CanParse(string ext) => ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public ParseResult Parse(string filePath)
    {
        try {
            using var doc = PdfDocument.Open(filePath);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            return new ParseResult {
                Content = sb.ToString(),
                SourceFileType = ".pdf",
                IsSuccess = true,
                CharCount = sb.Length
            };
        }
        catch (EncryptedPdfDocumentException) {
            return new ParseResult {
                IsSuccess = false,
                ErrorMessage = "PDF 文档已加密"
            };
        }
    }
}
```
- 扫描件检测：若 `CharCount < 50 && FileSize > 100KB` → 错误 "不支持扫描版"

**验证：**
- 正常 PDF 解析
- 加密 PDF 返回错误
- 扫描件检测正确

---

### 步骤 6：实现解析路由器

**指令：**
创建 `AI.DiffAssistant.Core/Parser/FileParserRouter.cs`:
```csharp
public class FileParserRouter
{
    private readonly List<IFileParser> _parsers = new();

    public ParseResult ParseFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var parser = _parsers.FirstOrDefault(p => p.CanParse(ext));
        if (parser == null)
            return new ParseResult { IsSuccess = false, ErrorMessage = $"不支持的文件格式: {ext}" };
        return parser.Parse(filePath);
    }
}
```

**验证：**
- txt → TextFileProcessor
- docx → DocxParser
- pdf → PdfParser
- 未知格式 → 错误

---

### 步骤 7：集成解析器到 FileProcessor

**指令：**
修改 `FileProcessor.cs`:
```csharp
private readonly FileParserRouter _router = new();

public async Task<FileProcessResult> ProcessFileAsync(string path)
{
    var ext = Path.GetExtension(path).ToLowerInvariant();
    if (IsRichTextFile(ext)) {
        var result = _router.ParseFile(path);
        // 截断处理...
        return new FileProcessResult { Content = result.Content, IsTruncated = ... };
    }
    // 纯文本处理...
}

private bool IsRichTextFile(string ext) =>
    ext is ".pdf" or ".docx";
```

**验证：**
- txt + txt 正常
- docx + pdf 正常
- 跨格式对比正常

---

## 阶段四：通知升级

### 步骤 8：Toast 通知替换 MessageBox

**指令：**
重写 `AI.DiffAssistant.Core/Notification/NotificationManager.cs`:
```csharp
using Microsoft.Toolkit.Uwp.Notifications;

public static class NotificationManager
{
    public static void ShowSuccess(string message)
    {
        new ToastContentBuilder()
            .AddArgument("action", "openResult")
            .AddText("分析完成")
            .AddText(message)
            .AddButton(new ToastButton()
                .SetContent("打开文件")
                .AddArgument("action", "openFile"))
            .Show();
    }

    public static void ShowError(string error)
    {
        new ToastContentBuilder()
            .AddText("分析失败")
            .AddText(error)
            .Show();
    }
}

// 注册激活回调（Program.cs）
ToastNotificationManagerCompat.OnActivated += args => {
    if (args.Argument == "action=openResult" || args.Argument == "action=openFile")
        Process.Start(new ProcessStartInfo(differenceMdPath) { UseShellExecute = true });
};
```

**验证：**
- 成功通知弹出（右下角，非弹窗）
- 点击通知打开 difference.md
- 错误通知显示具体原因
- 不抢占键盘焦点

---

## 阶段五：测试与构建

### 步骤 9：新增单元测试

**指令：**
在 `AI.DiffAssistant.Tests` 添加：
- DocxParser 测试（文本、表格、空文件）
- PdfParser 测试（正常、加密、扫描件）
- FileParserRouter 测试（路由、未知格式）
- Toast 通知测试

**验证：**
- `dotnet test` 全部通过

---

### 步骤 10：集成测试

**指令：**
测试场景：
1. txt + txt 对比
2. docx + docx 对比
3. pdf + pdf 对比
4. docx + pdf 跨格式对比
5. 加密 PDF 错误处理
6. 扫描件 PDF 错误处理

**验证：**
- 所有场景正常执行
- difference.md 正确生成
- Toast 通知正常显示

---

## 附录 A：新增文件清单

| 文件 | 路径 |
|------|------|
| ParseResult.cs | `Shared/Models/` |
| FileParseResult.cs | `Shared/Models/` |
| DocxParser.cs | `Core/Parser/` |
| PdfParser.cs | `Core/Parser/` |
| FileParserRouter.cs | `Core/Parser/` |
| app.manifest | `Cli/` |

## 附录 B：修改文件清单

| 文件 | 修改内容 |
|------|----------|
| AI.DiffAssistant.Core.csproj | 添加 3 个 NuGet 包 |
| AI.DiffAssistant.Cli.csproj | 改为 WinExe |
| FileProcessor.cs | 集成 FileParserRouter |
| NotificationManager.cs | 重写为 Toast 通知 |
| Program.cs | 添加 Toast 激活回调 |

---

> 实施计划版本: 2.0 (V1.1 only)
> 生成时间: 2025-12-29
