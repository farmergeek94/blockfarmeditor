# Block Farm Editor - Client Scripts RCL

A comprehensive Razor Class Library (RCL) containing all client-side components for the Block Farm Editor - a powerful visual page building solution for Umbraco CMS.

## Overview

The Block Farm Editor Client Scripts RCL provides a complete set of web components and interfaces that enable visual page building within Umbraco CMS. This library includes everything needed for content editors to create rich, structured pages using a drag-and-drop interface, while giving developers full control over block definitions and rendering.

## What's Included

This RCL contains four main client-side applications that work together to provide a seamless page building experience:

### 🎛️ Settings Dashboard
**Configuration management**
- Import/Export of definitions. 
- Layout Management

### 🏗️ Definitions Workspace
**Block definition management**
- Create and manage block definitions directly in document type workspace
- Support for both Partial Views and View Components
- Category organization with dynamic category creation
- Integrated file browser for view selection
- Auto-save integration with Umbraco workspace

### 📝 Property Editor
**Visual page editing interface**
- Modal-based visual page builder
- Drag-and-drop block arrangement
- Dynamic property editing with Umbraco's property system
- Multi-language support
- Real-time preview integration

### 🎨 Block Editor
**Front-end visual editing**
- WYSIWYG visual editing interface running in iframe
- Live preview with real-time updates
- Drag-and-drop block reordering
- Inline property editing
- Hierarchical layer management
- Nested block support

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Umbraco Backoffice                      │
├─────────────────┬─────────────────┬─────────────────────────┤
│ Settings        │ Document Type   │ Document Editor         │
│ Dashboard       │ Workspace       │                         │
│                 │                 │                         │
│ ┌─────────────┐ │ ┌─────────────┐ │ ┌─────────────────────┐ │
│ │  Layout     │ │ │ Definition  │ │ │   Property Editor   │ │
│ │ Management  │ │ │ Management  │ │ │                     │ │
│ └─────────────┘ │ └─────────────┘ │ │ ┌─────────────────┐ │ │
│                 │                 │ │ │   Block Editor  │ │ │
│                 │                 │ │ │   (in iframe)   │ │ │
│                 │                 │ │ └─────────────────┘ │ │
│                 │                 │ └─────────────────────┘ │
└─────────────────┴─────────────────┴─────────────────────────┘
```

## Key Features

### For Content Editors
- **Visual Page Building**: Drag-and-drop interface for creating structured pages
- **Real-time Preview**: See changes immediately without page refresh
- **Block Management**: Easily add, edit, reorder, and remove content blocks
- **Property Editing**: Edit block properties with familiar Umbraco property editors
- **Multi-language Support**: Full support for Umbraco's multi-language capabilities
- **Responsive Design**: Works seamlessly across desktop, tablet, and mobile devices

### For Developers
- **Flexible Block System**: Create custom blocks using Partial Views or View Components
- **Property Integration**: Leverage any Umbraco property editor for block properties
- **Category Organization**: Organize blocks into logical categories
- **Nested Blocks**: Support for container blocks that can hold other blocks
- **API Integration**: RESTful API for all block operations
- **Extensible Architecture**: Built with web components for maximum flexibility

### For Administrators
- **Configuration Dashboard**: Centralized configuration through Umbraco settings
- **Definition Management**: Manage block definitions at the document type level

## Technology Stack

- **Frontend Framework**: Lit 3.3.0 (Web Components)
- **Build Tool**: Vite 6.3+ with TypeScript support
- **UI Framework**: Umbraco UI Library (uui-* components)
- **Integration**: Umbraco 13+ Backoffice API
- **Communication**: PostMessage API for secure iframe communication
- **Architecture**: Modular web components with event-driven communication

## Getting Started

### Prerequisites
- Umbraco 17.0 or later
- .NET 10.0 or later

### Installation
1. Install the Block Farm Editor package through NuGet or Umbraco marketplace
2. The client scripts RCL will be automatically included
3. Create block definitions in your document types
4. Add Block Farm Editor property to your document types
5. Start building pages!

### Basic Workflow

#### 1. Create Block Definitions
1. Create or edit a document type marked as "Element"
2. Use the **Definitions** workspace tab
3. Configure rendering type (Partial View or View Component)
4. Set view path and category
5. Save the document type

#### 2. Configure Property Editor
1. Create or edit a document type for pages
2. Add a property with **Block Farm Editor** data type
3. Configure allowed block types if needed
4. Save the document type

#### 3. Build Pages
1. Create or edit content using the configured document type
2. Click **"Open Block Farm Editor"** on the property
3. Use the visual interface to:
   - Add blocks from the catalog
   - Drag and drop to reorder
   - Edit block properties
   - Preview changes in real-time
4. Save to apply changes

## Component Details

### Settings Dashboard (`/settings-dashboard/`)
- **Element**: `<blockfarmeditor-settings-dashboard>`
- **Purpose**: Layout management
- **Integration**: Umbraco Settings section
- **Build Output**: `wwwroot/settings-dashboard/dist/`

### Definitions Workspace (`/definitions-workspace/`)
- **Element**: `<blockfarmeditor-definitions>`
- **Purpose**: Block definition creation and management
- **Integration**: Document type workspace
- **Build Output**: `wwwroot/definitions-workspace/dist/`
- **Condition**: Only appears for element document types

### Property Editor (`/property-editor/`)
- **Element**: `<blockfarmeditor-page-propertyeditor>`
- **Purpose**: Visual page building interface
- **Integration**: Document property editor
- **Build Output**: `wwwroot/property-editor/dist/`
- **Components**: 
  - Main property editor
  - Editor modal (sidebar)
  - Add block modal
  - Properties modal

### Block Editor (`/block-editor/`)
- **Element**: `<block-area>`
- **Purpose**: Front-end visual editing interface
- **Integration**: Runs in iframe during preview
- **Build Output**: `wwwroot/block-editor/dist/`
- **Communication**: PostMessage with parent window

## API Endpoints

The client components communicate with these server endpoints:

### Block Definitions
- `GET /umbraco/blockfarmeditor/definitions` - Get definition by alias
- `GET /umbraco/blockfarmeditor/definitions/categories` - Get categories
- `POST /umbraco/blockfarmeditor/definitions/create` - Create definition
- `PUT /umbraco/blockfarmeditor/definitions/update/{id}` - Update definition

### Block Operations
- `POST /umbraco/blockfarmeditor/getblockdefinitions` - Get available blocks
- `POST /umbraco/blockfarmeditor/renderblock/{id}` - Render block HTML

## Performance

- **Lazy Loading**: Components load only when needed
- **Tree Shaking**: Optimized builds with unused code elimination
- **Source Maps**: Available for debugging
- **Bundle Splitting**: Separate bundles for optimal caching
- **Efficient Rendering**: Minimal DOM updates during editing

## Security

- **PostMessage Validation**: Secure communication between components
- **Origin Checking**: Validates message origins for security
- **Authentication**: Integrates with Umbraco's authentication system
- **Permission Checking**: Respects Umbraco's permission system

## Troubleshooting

### Common Issues

#### Components Not Loading
- Verify NuGet package is properly installed
- Check that wwwroot files are deployed
- Ensure Umbraco package is properly registered

#### Property Editor Not Working
- Confirm document is saved (not new)
- Check browser console for JavaScript errors
- Verify Block Farm Editor data type is configured

#### Visual Editor Issues
- Check iframe loading and PostMessage communication
- Verify preview mode is working in Umbraco
- Ensure block definitions exist for content types

## Support

For technical support, documentation, and updates:

- **Website**: [https://blockfarmeditor.com](https://blockfarmeditor.com)
- **Documentation**: [https://blockfarmeditor.com/documentation](https://blockfarmeditor.com/documentation)
- **Support**: [https://blockfarmeditor.com/about/contactus](https://blockfarmeditor.com/about/contactus)

## License

This component is part of the Block Farm Editor package. For support or issues, contact [https://blockfarmeditor.com/about/contactus](https://blockfarmeditor.com/about/contactus).

**Block Farm Editor** - Empowering content creators with visual page building tools for Umbraco CMS.
