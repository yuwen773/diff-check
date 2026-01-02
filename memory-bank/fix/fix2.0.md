# Fix 2.0 - Root Cause Notes

## Scope
- Toast "打开文件" click triggers CLI parameter error.
- Default theme should be Light, not Dark.

## Findings

### Issue 1: Toast "打开文件" triggers "参数中包含不存在的文件"

Observed error:
```
[2025-12-31 09:52:25.368] [Error] 参数解析失败: 参数中包含不存在的文件
[2025-12-31 09:52:25.387] [Warning] 通知 - 错误: 参数错误: 参数中包含不存在的文件
```

Root cause:
- The Toast action buttons only add arguments (`action=openFile`, `action=openFolder`) and do not use protocol activation or a direct shell open. See `src/AI.DiffAssistant.Core/Notification/NotificationManager.cs`.
- When a user clicks the Toast action, Windows re-activates the CLI app with those arguments (and possibly `-ToastActivated`), but the CLI always routes all args to `ArgsParser.Parse()` in `src/AI.DiffAssistant.Cli/Program.cs`.
- `ArgsParser.Parse()` assumes all arguments are file paths; if it receives `action=openFile` (or `-ToastActivated` + `action=openFile`) it fails `File.Exists(...)` and returns "参数中包含不存在的文件". See `src/AI.DiffAssistant.Cli/ArgsParser.cs`.

Impact:
- Clicking "打开文件" launches the CLI again, which treats Toast activation args as file paths and fails before any file open logic can run.

### Issue 2: Default theme is Dark

Root cause:
- App resources load the dark theme by default in `src/AI.DiffAssistant.GUI/App.xaml`:
  - `<ResourceDictionary Source="Themes/DarkTheme.xaml"/>`
- The code default is also set to Dark in `src/AI.DiffAssistant.GUI/App.xaml.cs`:
  - `private static bool _isDarkTheme = true;`

Impact:
- First launch (and any scenario without explicit user choice) shows Dark theme, not Light.

## References
- `src/AI.DiffAssistant.Core/Notification/NotificationManager.cs`
- `src/AI.DiffAssistant.Cli/Program.cs`
- `src/AI.DiffAssistant.Cli/ArgsParser.cs`
- `src/AI.DiffAssistant.GUI/App.xaml`
- `src/AI.DiffAssistant.GUI/App.xaml.cs`

## Fix Plan

### Fix 1: Toast buttons use protocol activation
- Update Toast success notification to use protocol activation for the notification click and action buttons, so Windows directly opens the target file/folder instead of re-launching the CLI with non-file arguments.
- Only add action buttons when `filePathToOpen` is present; map "打开文件" to the file URI and "打开文件夹" to the folder URI.
- Remove unused activation-argument handling in the notification path (no `AddArgument("action", ...)` for success).

Expected result:
- Clicking "打开文件/打开文件夹" opens the resource directly and does not trigger CLI argument parsing errors.

### Fix 2: Default theme is Light
- Change the default merged dictionary in `App.xaml` to `Themes/LightTheme.xaml`.
- Set `App.xaml.cs` default `_isDarkTheme` to `false`.

Expected result:
- First launch defaults to Light theme.

## Verification
- Build: `dotnet build src/AI.DiffAssistant.slnx`
- Test: `dotnet test src/AI.DiffAssistant.Tests`
- Manual:
  - Run analysis via right-click; click Toast "打开文件/打开文件夹" and confirm no "参数中包含不存在的文件" error.
  - Launch GUI and confirm the default theme is Light.
