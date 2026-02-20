# Block Farm Editor

[![Docs](https://img.shields.io/badge/docs-blockfarmeditor.com-blue)](https://blockfarmeditor.com/readme)
[![NuGet](https://img.shields.io/nuget/v/BlockFarmEditor.Umbraco.svg)](https://www.nuget.org/packages/BlockFarmEditor.Umbraco/)
[![NuGet downloads](https://img.shields.io/nuget/dt/BlockFarmEditor.Umbraco.svg)](https://www.nuget.org/packages/BlockFarmEditor.Umbraco/)
[![Issues](https://img.shields.io/github/issues/farmergeek94/blockfarmeditor.svg)](https://github.com/farmergeek94/blockfarmeditor/issues)
[![License](https://img.shields.io/github/license/farmergeek94/blockfarmeditor.svg)](https://github.com/farmergeek94/blockfarmeditor)

Block Farm Editor is a visual content editor for Umbraco that enables flexible, block-based content editing.

Build reusable content blocks and containers that can be managed and rendered throughout your website.

**Quick links**
- Docs: https://blockfarmeditor.com/readme
- Issue tracker: https://github.com/farmergeek94/blockfarmeditor/issues

## Table of contents
- [Install](#install)
- [Create a Block Area](#create-a-block-area)
- [Creating Blocks](#creating-blocks)
- [Key Features](#key-features)
- [History](#history)

## Install

```bash
dotnet add package BlockFarmEditor.Umbraco
```

Add `AddBlockFarmEditor()` to your `UmbracoBuilder`.

Docs: https://blockfarmeditor.com/readme

## Create a Block Area

Block Areas define where blocks can be placed in your templates. Use the `<block-area>` tag helper to define block areas in your templates.

Docs: https://blockfarmeditor.com/readme/#create-your-first-block-area

## Creating Blocks

### 1. Build your first block

Blocks are reusable components geared toward easily placing content in different areas in your templates. They can be used to create anything from simple text blocks to complex components with multiple properties and behaviors.

#### Build the Element Type

The initial properties of the block are built out using Umbraco DataTypes just like any other Umbraco Element Type.

Docs: https://blockfarmeditor.com/readme#building-first-block

#### Create the partial view

Create a partial view to display the properties. You can use standard Umbraco syntax including the Models Builder.

Docs: https://blockfarmeditor.com/readme/#create-the-partial-view

#### Optional ViewComponent Assembly Attribute

If you prefer to use a View Component for your display vs a direct partial view:

Docs: https://blockfarmeditor.com/readme/#optional-viewcomponent-assembly-attribute

#### Set up the Block Definition

Go back to the Block’s Element Type. Switch to the Definition tab to set up the Block Definition. This allows the Element Type to be selectable as a visual block.

Docs: https://blockfarmeditor.com/readme/#setup-the-block-definition

### 2. Defining Custom Block Property Configs

You are able to directly control the configuration passed into a DataType programmatically.

- Use the `BlockFarmEditorConfiguration` attribute to define the custom configuration for a property.

Docs: https://blockfarmeditor.com/readme/#defining-block-properties

### 3. Reusable Layouts

Save reusable page and block layouts to accelerate initial page creation.

Docs: https://blockfarmeditor.com/readme/layouts/

## Key Features

- **Flexible Property Editors:** Use existing Umbraco data types or property editors
- **Custom Configuration:** Create reusable configuration classes for property editors
- **Template Integration:** Simple tag helper syntax for rendering block areas
- **View Components Support:** Use either Razor views or View Components for rendering
- **Container Support:** Create nested block structures with container blocks

## History
### Recent releases
- **17.2.2**: Added the Definition Actions back in.
- **17.2.1**: Resolve client library static assets.
- **17.2.0**: Updated Umbraco to 17.2.0.
- **17.1.15**: Licensed under MIT.
- **17.1.14**: Styling updates and improvements.
- **17.1.13**: Resolved a boolean parse issue.
- **17.1.12**: Resolved Composition PropertyTypes not being resolved.
- **17.1.11**: Added in orphaned PropertyGroups.
- **17.1.10**: Resolved composition propertygroups.
- **17.1.9**: Resolved core nuget upgrade.
- **17.1.8**: Additional membership changes.

Full changelog: https://github.com/farmergeek94/blockfarmeditor/blob/main/CHANGELOG.md
