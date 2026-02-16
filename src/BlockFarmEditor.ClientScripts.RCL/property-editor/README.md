# Property Editor

A comprehensive Lit-based property editor for the Block Farm Editor within Umbraco CMS, providing a visual drag-and-drop interface for building pages with blocks.

## Overview

This property editor serves as the main interface for content editors to build pages using the Block Farm Editor. It provides a modal-based editing experience with drag-and-drop functionality, block management, and property editing capabilities, all integrated seamlessly within Umbraco's property editor system.

## Features

- **Modal-Based Editing**: Full-screen modal editing experience with iframe integration
- **Visual Block Builder**: Drag-and-drop interface for arranging blocks on pages
- **Block Management**: Add, edit, remove, and reorder blocks with visual feedback
- **Property Editing**: Dynamic property editor interface for block properties
- **Multi-language Support**: Culture-aware editing with language context
- **Preview Integration**: Real-time preview capabilities using Umbraco's preview service
- **Auto-save Integration**: Automatic saving with Umbraco's change detection
- **Responsive Design**: Optimized for various screen sizes and devices

## Project Structure

```
property-editor/
├── src/
│   ├── blockfarmeditor-editor.ts        # Main property editor component
│   ├── index.css                        # Global styles
│   ├── vite-env.d.ts                   # Vite type definitions
│   ├── components/                      # Modal components
│   │   ├── blockfarmeditor-modal.ts     # Main editor modal
│   │   ├── blockfarmeditor-add-block.ts # Add block modal
│   │   └── blockfarmeditor-properties.ts # Block properties modal
│   ├── helpers/                         # Utility classes
│   │   └── UrlHelper.ts                # URL manipulation utilities
│   ├── tokens/                          # Modal token definitions
│   │   ├── blockfarm-editor-modal.token.ts
│   │   ├── add-block-modal.token.ts
│   │   └── block-properties-modal.token.ts
│   └── assets/                          # Static assets
├── public/                              # Public assets
├── package.json                         # Dependencies and scripts
├── tsconfig.json                       # TypeScript configuration
├── vite.config.ts                      # Vite build configuration
└── README.md                           # This file
```

## Components

### BlockFarmEditorPropertyEditor (`blockfarmeditor-editor.ts`)

The main property editor component that integrates with Umbraco's property system.

#### Features:
- **Document Context Integration**: Observes document workspace changes
- **Language Support**: Multi-culture editing capabilities
- **Modal Management**: Handles opening and closing of the editor modal
- **Value Management**: Manages PageDefinition value changes
- **State Validation**: Prevents editing until document is saved

#### Key Properties:
- `value`: PageDefinition object containing page structure and blocks
- `elementId`: Unique identifier for this editor instance

### BlockFarmEditorModal (`blockfarmeditor-modal.ts`)

The main editing modal that provides the visual block editing interface.

#### Features:
- **Iframe Integration**: Embedded iframe for the visual editor
- **Message Handling**: PostMessage communication with the iframe
- **Modal Orchestration**: Manages sub-modals for adding blocks and editing properties
- **Preview Integration**: Integrates with Umbraco's preview service
- **Auto-initialization**: Automatically loads existing page data

#### Key Methods:
- `_handleIframeMessage()`: Processes messages from the visual editor
- `_saveChanges()`: Triggers save operation in the iframe
- `_requestShowLayers()`: Toggles visual layer display

### AddBlockModalElement (`blockfarmeditor-add-block.ts`)

Modal for selecting and adding new blocks to the page.

#### Features:
- **Block Catalog**: Displays available block definitions
- **Search Functionality**: Filter blocks by name or category
- **Category Organization**: Organized view of blocks by category
- **Visual Selection**: Rich visual interface for block selection
- **Filtered Results**: Respects allowed blocks configuration

#### Key Properties:
- `_blockDefinitions`: Available block definitions from server
- `_filteredDefinitions`: Filtered blocks based on search/category
- `_searchTerm`: Current search filter

### BlockPropertiesModalElement (`blockfarmeditor-properties.ts`)

Modal for editing individual block properties with dynamic property editors.

#### Features:
- **Dynamic Property Editors**: Generates appropriate editors for block properties
- **Property Extension Registry**: Uses Umbraco's property editor system
- **Type-aware Editing**: Proper editors based on property data types
- **Validation Support**: Built-in validation for property values
- **Context Preservation**: Maintains block context during editing

#### Key Properties:
- `_blockProperties`: Block property definitions
- `_properties`: Current property values
- `_loading`: Loading state for async operations

## Modal Tokens

### BlockFarmEditorModalToken
- **Type**: Sidebar modal, full size
- **Data**: PageDefinition, content unique ID, culture
- **Result**: Updated PageDefinition

### AddBlockModalToken
- **Type**: Dialog modal
- **Data**: Allowed blocks configuration
- **Result**: Selected block definition

### BlockPropertiesModalToken
- **Type**: Dialog modal
- **Data**: Block data and property definitions
- **Result**: Updated block properties

## API Integration

The property editor communicates with the following endpoints:

- `POST /umbraco/blockfarmeditor/getblockdefinitions/` - Retrieve available block definitions
- `POST /Preview/Index` - Umbraco's preview service for iframe content
- Various property-specific endpoints for dynamic property editing

## Development

### Prerequisites

- Node.js and npm
- TypeScript knowledge
- Familiarity with Lit framework and Umbraco property editors
- Understanding of Umbraco's modal and preview systems

### Installation

```bash
npm install
```

### Available Scripts

- `npm run dev` - Start development server with hot reload
- `npm run build` - Build for production (TypeScript compilation + Vite build)
- `npm run watch` - Build in watch mode for development
- `npm run preview` - Preview production build

### Building

The components build to `../wwwroot/property-editor/dist/` with the following outputs:
- `blockfarmeditor-editor.js` - Main property editor
- `blockfarmeditor-modal.js` - Editor modal
- `blockfarmeditor-add-block.js` - Add block modal
- `blockfarmeditor-properties.js` - Properties modal

## Integration Architecture

### Property Editor Registration
```typescript
// Registered as Umbraco property editor
{
  type: 'propertyEditorUi',
  alias: 'BlockFarmEditor.PropertyEditorUi.PageEditor',
  name: 'Block Farm Editor',
  element: () => import('./blockfarmeditor-editor.js')
}
```

### Modal System Integration
The editor uses Umbraco's modal system with route registration for:
- Main editor modal (sidebar, full-screen)
- Add block modal (dialog)
- Block properties modal (dialog)

### Context Integration
- **Document Workspace**: Observes document state and changes
- **Language Workspace**: Multi-culture support
- **Preview Service**: Iframe content management
- **Property Registry**: Dynamic property editor resolution

## Communication Flow

### Editor ↔ Iframe Communication
```typescript
// To iframe
{
  messageType: 'blockfarmeditor:initialize',
  data: PageDefinition,
  culture: string
}

// From iframe
{
  messageType: 'blockfarmeditor:save',
  data: PageDefinition
}
```

### Modal Communication
- Parent modal manages child modal lifecycle
- Results are passed through modal result system
- State synchronization between modals

## Styling

The components use Umbraco's UI library (`uui-*` components) with custom CSS:

- **Responsive Modal Design**: Adapts to different screen sizes
- **Visual Block Catalog**: Grid-based block selection interface
- **Property Editor Layout**: Consistent with Umbraco's property editing patterns
- **Loading States**: Visual feedback during async operations

## Error Handling

Comprehensive error handling includes:

- **Network Errors**: API call failures with user feedback
- **Modal Errors**: Graceful modal failure handling
- **Iframe Communication**: Timeout and error detection
- **Property Validation**: Real-time validation feedback
- **State Recovery**: Automatic recovery from invalid states

## Value Structure

### PageDefinition
```typescript
interface PageDefinition {
  unique: string;           // Unique page identifier
  type: string;            // Page type identifier
  blocks: BlockDefinition[]; // Array of page blocks
}
```

### BlockDefinition
```typescript
interface BlockDefinition {
  contentTypeKey: string;   // Content type GUID
  properties: object;      // Block property values
  uniquePath: string;      // Unique block identifier
  // Additional block metadata
}
```

## Performance Optimizations

- **Lazy Loading**: Components load on demand
- **Iframe Optimization**: Efficient iframe lifecycle management
- **State Management**: Minimal re-renders with state optimization
- **Bundle Splitting**: Separate builds for each modal component
- **Caching**: Appropriate caching of block definitions

## Dependencies

### Runtime Dependencies
- `lit`: ^3.3.0 - Web component framework

### Development Dependencies
- `@umbraco-cms/backoffice`: ^16.0.0 - Umbraco backoffice framework
- `typescript`: ~5.8.3 - TypeScript compiler
- `vite`: ^6.3.5 - Build tool and development server

## Usage

### Content Editor Workflow
1. **Navigate** to a document with a Block Farm Editor property
2. **Click** "Open Block Farm Editor" button
3. **Use** the visual interface to:
   - Add blocks from the block catalog
   - Drag and drop to reorder blocks
   - Edit block properties
   - Preview changes in real-time
4. **Save** to apply changes to the document

### Developer Integration
1. **Create** a property editor using the Block Farm Editor data type
2. **Configure** allowed blocks and settings
3. **Implement** block definitions for content types
4. **Customize** block rendering in the front-end

## Troubleshooting

### Common Issues
- **Modal not opening**: Check document save state
- **Iframe loading issues**: Verify preview service configuration
- **Block definitions not loading**: Check API endpoint accessibility
- **Property editors not working**: Verify property editor registrations

## License

This component is part of the Block Farm Editor package. For support or issues, contact [https://blockfarmeditor.com/about/contactus](https://blockfarmeditor.com/about/contactus).
