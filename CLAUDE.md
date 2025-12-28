# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AI Document Difference Assistant (AI Diff Assistant) - A Windows desktop application that integrates with Windows File Explorer to provide AI-powered semantic document comparison. Users right-click two files to get AI-generated diff summaries.

**Tech Stack:** C# + .NET 8 + WPF (AOT compiled to standalone .exe)

**Key Docs:** Reference `memory-bank/prd/prd1.0.md` for requirements and `memory-bank/tech-stack.md` for architecture.

## Build Commands

```bash
# AOT compile to standalone .exe
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

**Requirements:** .NET 8.0 Windows Desktop SDK, Visual Studio 2022

## Architecture

**Pattern:** Modular with MVVM

```
src/
├── AI.DiffAssistant.Core/       # Business logic
├── AI.DiffAssistant.GUI/        # WPF UI (Views, ViewModels, Converters)
├── AI.DiffAssistant.Shared/     # Shared models
└── AI.DiffAssistant.Cli/        # CLI entry point (silent mode)
```

## Critical Implementation Details

1. **Single Instance Enforcement:** Use Mutex to handle Windows multi-file selection (may launch multiple instances)
2. **Windows Integration:** Registry operations at `HKCU\Software\Classes\*\shell`
3. **File Encoding:** Support UTF-8, GBK, ASCII auto-detection
4. **File Truncation:** Auto-truncate at 15,000 chars if exceeded, preserve header
5. **Output Format:** Append results to `difference.md` with timestamp and status
6. **System Notification:** Toast notification on completion with click-to-open behavior

## Configuration Schema

Config stored as JSON (`config.json`):
```json
{
  "api": { "baseUrl": "...", "apiKey": "...", "model": "..." },
  "prompts": { "system": "..." },
  "settings": { "maxTokenLimit": 15000 }
}
```

## Performance Requirements

- Cold startup < 1 second
- Support text files only (.txt, .md, .cs, .js, .py, .json, etc.)

## MANDATORY RULES (Always Applied)

**IMPORTANT:**

- **Before writing any code, you MUST fully read `memory-bank/@architecture.md` (contains complete database structure)**
- **Before writing any code, you MUST fully read `memory-bank/@prdx.x.md` such as `prd1.0.md`**
- **After completing each major feature or milestone, you MUST update `memory-bank/@architecture.md`**
