# BlockFarmEditor.USync

uSync integration package for BlockFarmEditor that provides automatic synchronization of block definitions and layouts across Umbraco environments.

## Installation

### Prerequisites

- **Umbraco CMS**: Version 17+ (compatible with .NET 10)
- **BlockFarmEditor.Umbraco.Core**: Core BlockFarmEditor package
- **uSync**: Version 17+ for BackOffice integration

### NuGet Package Installation

```bash
dotnet add package BlockFarmEditor.USync
```

### Verification

After installation, verify the integration is working by:

1. Check uSync Dashboard for `BlockFarmEditorDefinition` and `BlockFarmEditorLayout` item types
2. Run a uSync export and check for BlockFarmEditor items in your uSync directory
3. Monitor Umbraco logs for any uSync-related messages during startup

## Overview

This package provides comprehensive uSync integration for BlockFarmEditor using both serializers and handlers for each entity type:

- **Block Definitions**: Handles block definition synchronization
- **Block Layouts**: Handles layout synchronization with complex JSON data preservation

All components are automatically registered and require no manual configuration.

## Features

- **Automatic Export/Import**: Both definitions and layouts sync during uSync operations
- **XML Storage**: Items stored as XML files in the uSync folder structure
- **Change Tracking**: Detailed change detection for all properties
- **Real-time Sync**: Notification support for immediate synchronization
- **Data Integrity**: Complex JSON layout data is preserved
- **Multiple Lookups**: Support for both key-based and alias-based lookups

## File Structure

After export, your uSync folder will contain:

```
uSync/v17/
├── BlockFarmEditorDefinition/
│   └── {definition-files}.config
└── BlockFarmEditorLayout/
    └── {layout-files}.config
```

## XML Examples

**Block Definition:**
```xml
<BlockFarmEditorDefinition Key="{guid}" Alias="{content-type-alias}">
  <Type>partial</Type>
  <ContentTypeAlias>alert</ContentTypeAlias>
  <ViewPath>~/Views/Partials/Alert.cshtml</ViewPath>
  <Category>Content</Category>
  <Enabled>true</Enabled>
</BlockFarmEditorDefinition>
```

**Block Layout:**
```xml
<BlockFarmEditorLayout Key="{guid}" Alias="{guid}">
  <Name>Hero Layout</Name>
  <Description>Hero section layout</Description>
  <Layout>{complex-json-layout-data}</Layout>
  <Category>Headers</Category>
  <Type>blockArea</Type>
  <Icon>icon-layout</Icon>
  <Enabled>true</Enabled>
</BlockFarmEditorLayout>
```

## Usage

1. Install the package
2. Restart Umbraco
3. Use uSync normally - BlockFarmEditor items will sync automatically

No configuration is required. The package integrates seamlessly with your existing uSync setup.

## Troubleshooting

**Items not syncing?**
- Verify BlockFarmEditor.Umbraco.Core and uSync are properly installed
- Check that items exist in your database
- Ensure uSync is working for other item types
- Check Umbraco logs for detailed error messages

**Missing item types in uSync dashboard?**
- Restart your Umbraco application
- Verify the package is properly referenced
- Check that all dependencies are up to date
