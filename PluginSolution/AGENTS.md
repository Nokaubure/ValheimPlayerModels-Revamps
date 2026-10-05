# Codex working tree rules

## Current files are always authoritative

- The current working tree on disk is the source of truth.
- Before modifying a file, inspect its current contents.
- The user may have manually edited files since Codex's previous turn.
- Never restore, reconstruct, or overwrite a file using a version from a previous Codex turn.
- Never discard or revert changes made by the user.
- Always apply new changes on top of the file's current state.
- If a file has changed since Codex last touched it, preserve those changes and modify the current version.



# Repository Guidelines

## Project Structure & Modules

This repository contains one C# plugin solution, `ValheimPlayerModels.sln`, with its project in `ValheimPlayerModels.csproj`. Core plugin, player model, patch, configuration, and physics code lives at the repository root. `Loaders/` contains avatar format loaders (including VRM and Valavtr); `StateBehaviours/` contains Unity state behavior components. `Properties/` holds generated settings and resource files. There is no separate test project or checked-in test suite.

## Build & Development

Build from this directory with:

- `dotnet build ValheimPlayerModels.sln -c Debug` — build the plugin configuration.
- `dotnet build ValheimPlayerModels.sln -c Release` — build a release assembly.
- `dotnet build ValheimPlayerModels.sln -c SDK` — build the SDK configuration.

The project targets .NET Framework 4.6.2 and uses NuGet packages for Valheim, Unity, and BepInEx references. For local development, set `GameDir`, `TargetInstallDir`, and, if needed, `PublicizeLocally` in an untracked `Directory.Build.props`; see `README.md` for an example. Review the project’s `PostBuild` target before building: it copies output to machine-specific paths.

## Coding Style & Naming

Follow the existing C# style: four spaces for indentation, braces on separate lines, and `PascalCase` for types and methods. Use `camelCase` for local variables and parameters; retain established Unity/BepInEx naming where APIs require it. Keep related loader and state behavior code in their existing folders. Avoid editing generated files under `Properties/` unless changing their source resource or settings definitions.

## Testing

No automated test framework or test project is currently configured. For code changes, build the relevant configuration and, when possible, verify the behavior in a local Valheim/BepInEx installation. Do not claim runtime validation unless it was performed.

## Commits & Pull Requests

Recent history uses short, direct subjects (for example, `Fix for scrollbar...`, `Reformat readme`, and `1.4.0 Release`), though conventions are not fully consistent. Use a concise imperative subject describing one change. Pull requests should explain the user-visible effect, list important implementation details, link related issues when applicable, and include screenshots or reproduction steps for UI changes. Mention which build configuration and in-game checks were completed.
