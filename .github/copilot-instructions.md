# GitHub Copilot Instructions for BlockFarmEditor

## Project Overview

BlockFarmEditor is a visual content editor for Umbraco that enables flexible, block-based content editing. It allows users to build reusable content blocks and containers that can be managed and rendered throughout their website.

- **Documentation**: https://blockfarmeditor.com/readme
- **NuGet**: https://www.nuget.org/packages/BlockFarmEditor.Umbraco/
- **Issue Tracker**: https://github.com/farmergeek94/blockfarmeditor/issues

## Solution Structure

| Project | Purpose |
|---------|---------|
| `BlockFarmEditor/` | Main Umbraco web application for development/testing |
| `src/BlockFarmEditor.Umbraco/` | Main NuGet package - Umbraco integration, tag helpers, controllers |
| `src/BlockFarmEditor.Umbraco.Core/` | Core library - models, DTOs, interfaces, database logic |
| `src/BlockFarmEditor.ClientScripts.RCL/` | Razor Class Library containing frontend Lit + Vite projects |
| `src/BlockFarmEditor.USync/` | uSync integration for serializing block definitions |

## Technology Stack

- **Backend**: .NET 10, C#, Umbraco CMS 17.x
- **Frontend**: Lit, Vite, TypeScript (4 separate projects in the RCL)
- **Database**: Uses Umbraco's database abstraction (NPoco)
- **Package Format**: NuGet

## Frontend

See [copilot-instructions-frontend.md](copilot-instructions-frontend.md) for detailed frontend development instructions.

## Code Style Guidelines

### C# Conventions

- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Use implicit usings
- Follow standard C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use async/await for asynchronous operations
- Prefer dependency injection via constructor injection

### Umbraco-Specific Patterns

- Use `IUmbracoBuilder` extension methods for registration (e.g., `AddBlockFarmEditor()`) instead of `IComposer` for startup composition
- Use tag helpers for rendering (e.g., `<block-area>`)
- Support both Razor views and View Components for block rendering

## Key Files

- `BlockFarmEditor/BlockFarmEditor/Configurations.cs` - Block configuration examples
- `src/BlockFarmEditor.Umbraco/BlockFarmEditorRegister.cs` - Service registration
- `src/BlockFarmEditor.Umbraco/TagHelpers/` - Razor tag helpers like `<block-area>`
- `src/BlockFarmEditor.Umbraco.Core/Models/` - Core domain models

## Versioning

When updating versions:

1. Update `<VersionPrefix>` in relevant `.csproj` files
2. Update `<PackageReleaseNotes>` with the change description
3. Add entry to `readme.md` History section

Projects to version together:
- `BlockFarmEditor.ClientScripts.RCL`
- `BlockFarmEditor.Umbraco`
- `BlockFarmEditor.Umbraco.Core`
- `BlockFarmEditor.USync`

See `.agents/skills/blockfarmeditor-versioning.md` for the versioning workflow.
## Common Tasks

### Creating a New Tag Helper

1. Add tag helper class in `src/BlockFarmEditor.Umbraco/TagHelpers/`
2. Follow the `<block-area>` pattern for implementation
3. Document usage in the docs site

### Modifying Frontend Components

1. Navigate to the appropriate project in `src/BlockFarmEditor.ClientScripts.RCL/`
2. Run `npm run watch` for watch mode during development
3. Run `npm run build` before committing

See `copilot-instructions-frontend.md` for more frontend development details.

## Build & Publish

```bash
# Build all projects
dotnet build BlockFarmEditor.slnx

# Create NuGet packages
dotnet pack src/BlockFarmEditor.Umbraco/BlockFarmEditor.Umbraco.csproj

# Packages output to src/ directory
```

## Notes for AI Assistants

- This is an **Umbraco CMS extension** - understand Umbraco concepts like Element Types, Property Editors, Data Types
- The project uses **Umbraco 17.x** which is built on **.NET 10**
- Frontend code uses **Lit** (not React/Vue/Angular) for web components
- Block definitions are configured through Umbraco's backoffice UI
- Tag helpers like `<block-area>` are the primary rendering mechanism
