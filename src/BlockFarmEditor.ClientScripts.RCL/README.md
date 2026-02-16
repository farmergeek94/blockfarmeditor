# Block Farm Editor - Client Scripts RCL

This README covers frontend setup and development workflows for the client scripts Razor Class Library (RCL).

## Prerequisites

- Node.js LTS and npm
- .NET 10 (or later) to run the Umbraco site
- An Umbraco solution that includes the Block Farm Editor projects

## Projects in this RCL

Four Lit + Vite projects live under this folder:

- [src/BlockFarmEditor.ClientScripts.RCL/settings-dashboard](src/BlockFarmEditor.ClientScripts.RCL/settings-dashboard) (Settings dashboard)
- [src/BlockFarmEditor.ClientScripts.RCL/definitions-workspace](src/BlockFarmEditor.ClientScripts.RCL/definitions-workspace) (Definitions workspace)
- [src/BlockFarmEditor.ClientScripts.RCL/property-editor](src/BlockFarmEditor.ClientScripts.RCL/property-editor) (Property editor)
- [src/BlockFarmEditor.ClientScripts.RCL/block-editor](src/BlockFarmEditor.ClientScripts.RCL/block-editor) (Block editor)

Each project builds to its corresponding output folder under `wwwroot/<project>/dist/` so the Umbraco package can serve the assets.

## One-time install

Pick one of these options.

Option A: install per project

```bash
cd settings-dashboard
npm install

cd ../definitions-workspace
npm install

cd ../property-editor
npm install

cd ../block-editor
npm install
```

Option B: run the helper script that installs and builds everything

- Windows: [src/BlockFarmEditor.ClientScripts.RCL/build-frontend.ps1](src/BlockFarmEditor.ClientScripts.RCL/build-frontend.ps1)
- macOS/Linux: [src/BlockFarmEditor.ClientScripts.RCL/build-frontend.sh](src/BlockFarmEditor.ClientScripts.RCL/build-frontend.sh)

## Development workflows

### Watch builds for Umbraco integration (recommended)

Use watch mode when you want the Umbraco site to load the latest assets from `wwwroot`.

```bash
cd settings-dashboard
npm run watch
```

Repeat `npm run watch` in each project you are actively editing. Each watcher rebuilds into `wwwroot/<project>/dist/` so the Umbraco backoffice picks up changes after refresh.

### Vite dev server (isolated UI work)

If you want a standalone dev server for quick UI iteration, use:

```bash
npm run dev
```

Note: the dev server is not wired into Umbraco by default. For backoffice integration, prefer watch mode.

### Production build

```bash
npm run build
```

## Running the Umbraco site

Start the .NET project (from the solution root) so Umbraco can serve the built assets. Any standard `dotnet run` workflow for the Block Farm Editor site is fine.

## Troubleshooting

- If builds do not update in the backoffice, confirm the watcher is running in the correct project folder.
- If `npm run watch` fails, remove `node_modules` in that project and re-run `npm install`.
- If the Umbraco UI cannot find the assets, confirm that `wwwroot/<project>/dist/` exists and contains the latest build output.
