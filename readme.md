# Block Farm Editor

## Overview

Block Farm Editor is a visual content editor that enables flexible, block-based content editing. Build reusable content blocks and containers that can be easily managed and rendered throughout your Umbraco website.

## Issue Tracking

Use GitHub Issues for bugs, feature requests, and questions:
https://github.com/farmergeek94/blockfarmeditor/issues

## Initial Install

```
dotnet add package BlockFarmEditor.Umbraco
```

Add ```AddBlockFarmEditor()``` to your UmbracoBuilder.
[https://blockfarmeditor.com/readme](https://blockfarmeditor.com/readme)

## Create a Block Area
Block Areas are used to define where blocks can be placed in your templates. Use the <block-area> tag helper to define block areas in your templates.

[https://blockfarmeditor.com/readme/#create-your-first-block-area](https://blockfarmeditor.com/readme/#create-your-first-block-area)

## Creating Blocks

### 1. Build your first block

Blocks are reusable components geared towards easily place content in different areas in your templates. They can be used to create anything from simple text blocks to complex components with multiple properties and behaviors.

#### Build the Element Type
The intial properties of the block are built out using Umbraco DataTypes just like any other Umbraco Element Type.
[https://blockfarmeditor.com/readme#building-first-block](https://blockfarmeditor.com/readme#building-first-block)

#### Create the partial view
Create a partial view with which to display the properties. You can use standard Umbraco Syntax including the Models Builder.

[https://blockfarmeditor.com/readme/#create-the-partial-view](https://blockfarmeditor.com/readme/#create-the-partial-view)

#### Optional ViewComponent Assembly Attribute
If you prefer to use a viewcomponent for your display vs a direct partial view
[https://blockfarmeditor.com/readme/#optional-viewcomponent-assembly-attribute](https://blockfarmeditor.com/readme/#optional-viewcomponent-assembly-attribute)

#### Setup the Block Definition
Go back to the Block’s Element Type. Switch to the Definition tab in order to setup the Block Definition. This is what will allow the Element Type to be selectedable as visual block.

[https://blockfarmeditor.com/readme/#setup-the-block-definition](https://blockfarmeditor.com/readme/#setup-the-block-definition)

### 2. Defining Custom Block Property Configs
You are able to directly control the configuration passed into a DataType programatically.

- Use the ```BlockFarmEditorConfiguration``` attribute to define the custom configuration for a property.

[https://blockfarmeditor.com/readme/#defining-block-properties](https://blockfarmeditor.com/readme/#defining-block-properties)

### 3. Reusable Layouts
Save reusable page and block layouts to accelerate initial page creation.

[https://blockfarmeditor.com/readme/layouts/](https://blockfarmeditor.com/readme/layouts)

## Key Features
* **Flexible Property Editors:** Use existing Umbraco data types or property editors
* **Custom Configuration:** Create reusable configuration classes for property editors
* **Template Integration:** Simple tag helper syntax for rendering block areas
* **View Components Support:** Use either Razor views or View Components for rendering
* **Container Support:** Create nested block structures with container blocks

## History

- **17.1.15**: Licensed under MIT.

- **17.1.14**: Styling updates and improvements.

- **17.1.13**: Resolved a boolean parse issue.
- **17.1.12**: Resolved Composition PropertyTypes not being resolved.
- **17.1.11**: Added in orphaned PropertyGroups.
- **17.1.10**: Resolved composition propertygroups.
- **17.1.9**: Resolved core nuget upgrade.
- **17.1.8**: Additional membership changes.
- **17.1.7**: Resolved an issue for logged in members.
- **17.1.6**: Resolved a position issue.
- **17.1.5**: Locking down some more styles.
- **17.1.4**: Changed some styling.
- **17.1.3**: Resolved an issue with saving block definitions.
- **17.1.2**:
	- Bug fixes related to layout saving.
	- Minor enhancements to layout handling.
- **17.1.0**: Added support for saving layouts.
- **17.0.1**: Converted layout handling to use `IEntity`.
- **17.0.0**: Updated for compatibility with Umbraco 17.
- **2.0.0**: Switched to using Element Document Types for blocks.
- **1.2.0**:
	- Switched to using data types instead of property editors directly.
	- Added `allowed-blocks` support to the tag helper.
- **1.1.2**: Resolved an issue with the block list.
- **1.1.1**: Fixed a movement issue with sibling areas.
- **1.1.0**:
	- Added the “show layers” feature.
	- Stabilized block movement behavior.
- **1.0.4**: Fully integrated Umbraco value editors and converters.
- **1.0.3**: Added parent block editing functionality.
- **1.0.2**:
	- Implemented the `AddBlockFarmEditor` method.
	- Removed the Composer (to be reintroduced in a separate project).
- **1.0.0**: Initial release for testing.