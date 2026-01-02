# Feature 1.0 Progress

## 实施记录
- 新增品牌资源并用于输出：`src/AI.DiffAssistant.GUI/Assets/diff-check.ico`、`src/AI.DiffAssistant.GUI/Assets/diff-check.png`，同时拷贝到 `src/AI.DiffAssistant.Cli/Assets/`，并确保发布/构建输出根目录包含图标文件。
- Toast 通知加入应用图标展示，使用 `diff-check.png` 作为 App Logo。
- 右键菜单改为 `diff-check`，更新注册表主键并清理旧键，图标改为 `diff-check.ico`。
- GUI 文案替换为 `diff-check`（标题与提示文案）。
- CLI/GUI 启动路径与右键菜单路径调整为新可执行文件名。
- 新增构建/发布后可执行文件重命名复制：`diff-check.exe` 与 `diff-check-cli.exe`。
- 配置与日志路径改名并迁移：默认配置路径 `%APPDATA%\\diff-check\\config.json`，日志路径 `%TEMP%\\diff-check.log`，旧配置自动迁移。
- 命令行使用说明更新为 `diff-check-cli.exe`，相关测试同步更新。

## 验证
- 构建：`dotnet build src/AI.DiffAssistant.slnx`（成功）
- 测试：`dotnet test src/AI.DiffAssistant.Tests`（128/128 通过）
- 手动验证待执行：
  - 右键菜单显示 `diff-check` 且带图标
  - Toast 成功通知展示图标，点击按钮能打开文件/文件夹
  - GUI 默认文案为 `diff-check`
