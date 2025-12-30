1. # 产品需求文档：AI 文档差异助手 (AI Diff Assistant) - V1.1

   > **版本变更记录**
   > *   **V1.0**: 初始版本，定义配置中心与纯文本静默对比流程。
   > *   **V1.1 (Current)**: 针对 MVP 反馈迭代。新增 **PDF/Word 文本解析**支持，并将交互从弹窗升级为 **Windows 原生 Toast 通知**。

   ## 1. 综述 (Overview)

   ### 1.1 项目背景
   MVP 版本已验证了“右键静默调用 AI 对比”的核心价值。V1.1 版本旨在解决用户反馈的痛点：文件格式支持单一导致使用场景受限，以及 `MessageBox` 弹窗打断工作流的问题。本版本将通过引入解析库和原生通知 API，进一步提升“无感化”的高效体验。

   ### 1.2 核心业务流程 (Updated)
   流程大体不变，但在 **阶段二：右键静默执行** 中增加了“格式预处理”环节。

   1.  **启动**: 右键触发 -> 2. **预处理**: 识别后缀 (.txt/.docx/.pdf) 并提取纯文本 -> 3. **截断**: 长度校验 -> 4. **分析**: AI 对比 -> 5. **反馈**: 原生 Toast 通知。

   ---

   ## 2. 用户故事详述 (User Stories)

   ### 阶段二：右键静默执行 (Silent Execution)

   *(注：阶段一配置中心逻辑无变更，此处省略，重点描述升级后的核心逻辑)*

   ---

   #### **US-03 (V1.1): 多格式解析与智能分析逻辑**
   *   **价值陈述**:
       *   **作为** 用户
       *   **我希望** 能直接选中 Word 合同或 PDF 报告进行对比，而不需要手动转成 txt
       *   **以便于** 覆盖更多真实办公场景（合同比对、简历筛选等）
   *   **业务规则与逻辑 (Business Logic)**:
       1.  **文件类型路由**:
           *   程序接收文件路径后，读取扩展名（不区分大小写）。
       2.  **解析策略 (Parsing Strategy)**:
           *   **纯文本 (.txt, .md, .cs, .json, .xml 等)**: 按 UTF-8 读取原始内容。
           *   **Word (.docx)**:
               *   **逻辑**: 仅解析 `MainDocumentPart` 中的文本段落。
               *   **忽略**: 忽略页眉、页脚、批注、图片、表格结构（仅取表格内文字）。
           *   **PDF (.pdf)**:
               *   **逻辑**: 按页顺序提取文本层。
               *   **异常处理 A (加密)**: 若文档受密码保护，捕获异常，停止对比，触发“分析失败”通知（提示：文档已加密）。
               *   **异常处理 B (扫描件)**: 若提取出的文本总长度 < 50 字符且文件体积 > 100KB，视为扫描件。停止对比，触发“分析失败”通知（提示：不支持扫描版/纯图片 PDF）。
       3.  **后续流程**:
           *   提取后的文本流进入原有的 **自动截断 (15k 字符)** -> **AI Prompt 组装** -> **结果写入** 流程。
   *   **验收标准 (Acceptance Criteria)**:
       *   **场景1: Word 文档对比**
           *   **GIVEN** 两个排版复杂的 .docx 合同
           *   **WHEN** 右键触发分析
           *   **THEN** 生成的 Markdown 仅包含文本内容的差异，忽略了字体颜色或段落间距的变化。
       *   **场景2: 扫描版 PDF**
           *   **GIVEN** 选中纯图片扫描的 PDF
           *   **WHEN** 右键触发分析
           *   **THEN** 不生成结果，收到通知提示“不支持扫描版”。

   ---

   #### **US-04 (V1.1): Windows 原生通知反馈 (Native Toast)**
   *   **价值陈述**:
       *   **作为** 用户
       *   **我希望** 结果通知以系统原生方式滑入右下角，不抢占焦点
       *   **以便于** 我可以继续当前工作，仅在需要时查看结果
   *   **业务规则与逻辑 (Business Logic)**:
       1.  **视觉样式**:
           *   使用 Windows 10/11 标准 Toast 样式。
           *   包含应用 Logo（如有）、标题（加粗）、正文。
       2.  **通知类型**:
           *   **成功**:
               *   标题: **分析完成**
               *   正文: "已生成对比报告 ([文件A] vs [文件B])"
               *   **交互**: 点击通知体 -> 默认关联程序打开 `difference.md`。
               *   **按钮 (Action)**: [打开文件] [打开所在文件夹]
           *   **失败**:
               *   标题: **分析失败**
               *   正文: 具体错误原因 (e.g., "API Key 额度不足", "PDF 无法读取").
       3.  **持久性**: 通知显示约 5-7 秒后自动收入通知中心，不强制用户关闭。
   *   **验收标准 (Acceptance Criteria)**:
       *   **GIVEN** 分析完成
       *   **WHEN** 通弹出
       *   **THEN** 屏幕焦点**未**被强制抢占（我打字不会被打断），点击通知能正确打开文件。

   ---

   ## 3. 技术实现概要 (Technical Implementation Brief) - C# 特别版

   针对您的 C# 技术栈，以下是实现 V1.1 的具体依赖和关键代码路径：

   *   **项目配置 (.csproj)**:
       *   必须指定 TargetFramework 为 Windows 特定版本以使用 Toast API：
           ```xml
           <TargetFramework>net10.0-windows10.0.17763.0</TargetFramework>
           ```

   *   **NuGet 依赖管理**:
       1.  **Word 解析**: `DocumentFormat.OpenXml` (微软官方库，稳定高效)。
       2.  **PDF 解析**: `PdfPig` (UglyToad.PdfPig) - MIT 协议，轻量级，无需非托管 DLL。
       3.  **通知系统**: `Microsoft.Toolkit.Uwp.Notifications` (CommunityToolkit)。

   *   **代码片段参考**:

       **1. Word 文本提取**:
       ```csharp
       using DocumentFormat.OpenXml.Packaging;
       
       public static string ReadDocx(string path) {
           using var doc = WordprocessingDocument.Open(path, false);
           return doc.MainDocumentPart.Document.Body.InnerText; 
           // 进阶：如果 InnerText 连在一起没换行，需遍历 Paragraphs 拼接 Environment.NewLine
       }
       ```

       **2. PDF 文本提取**:
       ```csharp
       using UglyToad.PdfPig;
       
       public static string ReadPdf(string path) {
           using var document = PdfDocument.Open(path);
           // 简单提取每一页的文本
           var textBuilder = new StringBuilder();
           foreach (var page in document.GetPages()) {
               textBuilder.AppendLine(page.Text);
           }
           return textBuilder.ToString();
       }
       ```

       **3. 发送 Toast 通知**:
       ```csharp
       using Microsoft.Toolkit.Uwp.Notifications;
       
       new ToastContentBuilder()
           .AddText("分析完成")
           .AddText("报告已追加至 difference.md")
           // 点击通知本体打开文件
           .SetProtocolActivation(new Uri($"file:///{filePath.Replace("\\", "/")}"))
           .Show();
       ```

   ## 4. 约束与边界 (Constraints)

   *   **OCR 限制**: 明确不支持 OCR（光学字符识别）。这意味着图片格式的 PDF 或 Word 中的截图无法被对比。
   *   **格式丢失**: 用户需知晓，基于纯文本的对比会丢失表格结构信息（例如表格的一行可能会变成一行文本），可能影响对复杂排版文档的理解。
   *   **系统版本**: 原生 Toast 通知要求 Windows 10 (Build 17763) 或更高版本。Win7 用户可能无法看到通知（需做降级处理或放弃支持）。