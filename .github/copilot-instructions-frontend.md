# Frontend Development Instructions for BlockFarmEditor

## Overview

The frontend code lives in `src/BlockFarmEditor.ClientScripts.RCL/` as a Razor Class Library containing four Lit + Vite projects.

## Projects

| Project | Purpose | Output |
|---------|---------|--------|
| `settings-dashboard/` | Settings dashboard UI | `wwwroot/settings-dashboard/dist/` |
| `definitions-workspace/` | Definitions workspace UI | `wwwroot/definitions-workspace/dist/` |
| `property-editor/` | Property editor UI | `wwwroot/property-editor/dist/` |
| `block-editor/` | Block editor UI | `wwwroot/block-editor/dist/` |

## Technology Stack

- **Framework**: Lit (web components)
- **Build Tool**: Vite
- **Language**: TypeScript (strict mode enabled)
- **UI Patterns**: Umbraco Backoffice UI conventions

## Build Commands

### Install All Dependencies

```bash
cd src/BlockFarmEditor.ClientScripts.RCL
./build-frontend.ps1  # Windows
./build-frontend.sh   # macOS/Linux
```

### Per-Project Commands

```bash
cd src/BlockFarmEditor.ClientScripts.RCL/<project-name>

# Install dependencies
npm install

# Development with watch mode
npm run watch

# Production build
npm run build
```

## Code Conventions

### Lit Component Patterns

- Use `@customElement('element-name')` decorator for component registration
- Use `@property()` for reactive public properties
- Use `@state()` for internal reactive state
- Prefix private properties with underscore

### Example Component Structure

```typescript
import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';

@customElement('my-component')
export class MyComponent extends LitElement {
  static styles = css`
    :host {
      display: block;
    }
  `;

  @property({ type: String })
  label = '';

  @state()
  private _isOpen = false;

  render() {
    return html`<div>${this.label}</div>`;
  }
}
```

### Naming Conventions

- Component files: `kebab-case.ts` (e.g., `block-picker.ts`)
- Component elements: `kebab-case` (e.g., `<block-picker>`)
- Classes: `PascalCase` (e.g., `BlockPicker`)
- Properties/methods: `camelCase`

## Shared Code

- `helpers/` - Shared utility functions
- `models/` - Shared TypeScript interfaces and types

## Development Workflow

1. Start the Umbraco site: `dotnet run` from `BlockFarmEditor/`
2. Watch the frontend project: `npm run watch` in the relevant project
3. Changes hot-reload automatically
4. Run `npm run build` before committing

## Notes for AI Assistants

- Use **Lit** syntax, not React/Vue/Angular
- Follow Umbraco Backoffice UI patterns for consistency
- Each project builds independently to its own `dist/` folder
- The RCL serves static assets via `_content/BlockFarmEditor.ClientScripts.RCL/`
