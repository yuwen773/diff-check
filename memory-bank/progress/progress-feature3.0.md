# Feature 3.0 进度记录 - 用户下载可用版本 & 关于页面精简

## 完成时间
2026-01-02

## 实施概述
完成稳定版下载列表与关于页面字段精简，用户可在 GUI 中查看稳定版版本信息并跳转下载，关于页面仅保留五项字段。

## 主要实现内容

### 1. 版本下载模型与服务
- **文件**: `src/AI.DiffAssistant.Shared/Models/ReleaseInfo.cs`
- **新增**: 版本信息模型（版本号、发布日期、平台、摘要、下载地址）
- **文件**: `src/AI.DiffAssistant.Core/Release/ReleaseService.cs`
- **功能**: 调用 GitHub Releases API，过滤预发布/草稿，仅保留稳定版并按发布日期降序排序

### 2. GUI 版本下载页
- **文件**: `src/AI.DiffAssistant.GUI/MainWindow.xaml`
- **变更**: 新增“版本下载”页签与版本列表，支持刷新与下载按钮
- **文件**: `src/AI.DiffAssistant.GUI/ViewModels/MainViewModel.cs`
- **变更**: 新增版本列表加载与下载命令，错误提示与加载状态

### 3. 关于页面精简
- **文件**: `src/AI.DiffAssistant.GUI/Views/AboutWindow.xaml`
- **变更**: 内容仅保留版本号、作者、官网、版权、更新渠道；官网更新为仓库地址
- **文件**: `src/AI.DiffAssistant.GUI/Views/AboutWindow.xaml.cs`
- **变更**: 版本信息仅显示版本号

## 验证测试

### 自动化测试
- `dotnet test src/AI.DiffAssistant.Tests`
- 结果：通过（130 tests）
- 警告：`EncodingDetector.cs` CA2022、`ResultWriterTests.cs` CS8604

### 手动验证
- 未执行（GUI 视觉与分辨率检查未进行）
