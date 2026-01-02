<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# Repository Guidelines

## Project Structure & Module Organization

- `src/AI.DiffAssistant.Core/`: Core diff logic, config, file IO, and notifications.
- `src/AI.DiffAssistant.GUI/`: WPF UI (Views, ViewModels).
- `src/AI.DiffAssistant.Cli/`: CLI entry point for silent/background runs.
- `src/AI.DiffAssistant.Shared/`: Shared models used by all apps.
- `src/AI.DiffAssistant.Tests/`: xUnit test project.
- `test/`: Sample input files for manual validation.
- `src/**/bin`, `src/**/obj`, `src/publish/`: Build outputs; do not edit by hand.

## Build, Test, and Development Commands

- `dotnet build src/AI.DiffAssistant.slnx`: Build all projects.
- `dotnet run --project src/AI.DiffAssistant.GUI`: Run the WPF app.
- `dotnet run --project src/AI.DiffAssistant.Cli -- <args>`: Run the CLI with arguments.
- `dotnet test src/AI.DiffAssistant.Tests`: Execute the xUnit test suite.
- `dotnet publish -c Release -r win-x64 --self-contained false -o publish`: AOT publish a Windows x64 release (outputs to `publish/`).

## Coding Style & Naming Conventions

- C# with `Nullable` and `ImplicitUsings` enabled; keep nullability warnings clean.
- Indentation is 4 spaces; use standard C# brace style.
- Naming: PascalCase for types/methods, camelCase for locals/parameters, `I` prefix for interfaces.

## Testing Guidelines

- Frameworks: xUnit + `Microsoft.NET.Test.Sdk`; `WireMock.Net` for HTTP scenarios.
- Test files follow `*Tests.cs` naming (for example, `ArgsParserTests.cs`).
- Optional coverage: `dotnet test --collect:"XPlat Code Coverage"`.

## Commit & Pull Request Guidelines

- Commit history favors short, single-line summaries (often Chinese), no conventional prefixes.
- PRs should include a concise description, test commands run, and screenshots for UI changes.

## Configuration & Security

- `config.json` contains API keys and prompts; keep it local and out of version control.
- Registry integration uses `HKCU\Software\Classes\*\shell`; verify on Windows only.

## Agent-Specific Instructions

- Before major feature work, read `memory-bank/prd/prd2.0.md` and `memory-bank/@architecture.md`.
- After significant architecture changes, update `memory-bank/@architecture.md`.
