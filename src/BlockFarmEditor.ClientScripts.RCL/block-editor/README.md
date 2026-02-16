# Block Editor

A Lit-based front-end visual editor for the Block Farm Editor, providing an interactive drag-and-drop interface for building and editing pages with blocks directly in the browser preview.

## Overview

The Block Editor is the client-side visual editing interface that runs within an iframe in Umbraco's preview mode. It provides content editors with a WYSIWYG experience for building pages using blocks, complete with drag-and-drop functionality, inline editing, block management, and real-time preview updates.

## Features

- **Visual Block Editing**: WYSIWYG interface for building pages with blocks
- **Drag & Drop**: Intuitive drag-and-drop interface for reordering blocks
- **Live Preview**: Real-time updates while editing without page refresh
- **Block Management**: Add, edit, delete, and reorder blocks visually
- **Nested Block Support**: Full support for blocks within blocks (containers)
- **Block Actions**: Context-sensitive actions for each block (edit, delete, move)
- **Layer Management**: Visual layers panel for complex page structures
- **Property Editing**: Direct integration with Umbraco's property editors
- **Responsive Design**: Works across different screen sizes and devices
- **PostMessage Communication**: Secure communication with parent Umbraco interface

## Project Structure

```
block-editor/
├── src/
│   ├── front-end-elements.ts           # Main entry point and block area component
│   ├── index.css                       # Global styles for the editor
│   ├── vite-env.d.ts                  # Vite type definitions
│   ├── components/                     # Visual editing components
│   │   ├── Block.ts                    # Individual block component
│   │   ├── BlockActions.ts             # Block action buttons (edit, delete, move)
│   │   ├── AddBlock.ts                 # Add block interface
│   │   ├── BlockProperties.ts          # Block properties editor
│   │   ├── BlockLayers.ts              # Layers panel component
│   │   ├── BlockLayerItem.ts           # Individual layer item
│   │   └── BlockTypeElement.ts         # Base class for block elements
│   ├── helpers/                        # Utility functions
│   │   ├── Actions.ts                  # Message passing helpers
│   │   └── GlobalListeners.ts          # Global event management
│   ├── models/                         # Type definitions
│   │   └── RenderedBlock.ts            # Rendered block data structure
│   └── assets/                         # Static assets
├── public/                             # Public assets
├── package.json                        # Dependencies and scripts
├── tsconfig.json                       # TypeScript configuration
├── vite.config.ts                      # Vite build configuration
└── README.md                           # This file
```

## Components

### BlockArea (`front-end-elements.ts`)

The main container component that manages the overall block editing experience.

#### Features:
- **Block Rendering**: Fetches and renders block HTML from the server
- **Event Coordination**: Central event handling for all block operations
- **Data Management**: Manages block data and page structure
- **Message Handling**: Communicates with parent window via PostMessage
- **Block Sorting**: Maintains proper block order and hierarchy

#### Key Methods:
- `blockFarmEditorLoad()`: Initializes the editor with existing page data
- `retrieveBlockHtml()`: Fetches rendered HTML for blocks from server
- `blockFarmEditorEventHandler()`: Processes block manipulation events

### Block (`Block.ts`)

Individual block component that wraps each rendered block with editing capabilities.

#### Features:
- **Visual Overlay**: Provides editing interface overlay on blocks
- **Hover Effects**: Visual feedback during interaction
- **Context Actions**: Access to block-specific actions
- **Property Access**: Direct access to block properties
- **Parent Tracking**: Maintains parent-child relationships

#### Key Properties:
- `contentTypeKey`: Umbraco content type identifier
- `index`: Position within parent container
- `hasProperties`: Whether block has editable properties
- `html`: Rendered block HTML content
- `parentUniquePath`: Parent block identifier

### BlockActions (`BlockActions.ts`)

Provides action buttons and controls for individual blocks.

#### Features:
- **Edit Properties**: Opens property editor for the block
- **Delete Block**: Removes block from page
- **Move Block**: Initiates drag-and-drop move operation
- **Parent Properties**: Access to parent block properties
- **Context Sensitivity**: Shows relevant actions based on block state

#### Key Actions:
- `blockDelete()`: Removes block and updates page structure
- `blockProperties()`: Opens property editor modal
- `blockMoveStart()`: Begins block moving operation

### AddBlock (`AddBlock.ts`)

Interface for adding new blocks to the page.

#### Features:
- **Block Catalog**: Access to available block types
- **Position Awareness**: Adds blocks at correct positions
- **Parent Context**: Respects parent block constraints
- **Visual Placement**: Clear visual indication of insertion points

#### Key Properties:
- `index`: Position where new block will be inserted
- `empty`: Whether this is an empty block area
- `_parentBlocks`: Parent block hierarchy for context

### BlockLayers (`BlockLayers.ts`)

Side panel showing hierarchical view of all blocks on the page.

#### Features:
- **Hierarchical View**: Tree view of page structure
- **Quick Navigation**: Click to select blocks
- **Visual Hierarchy**: Clear indication of nesting levels
- **Bulk Operations**: Multi-select and bulk actions

### BlockProperties (`BlockProperties.ts`)

Inline property editing interface for blocks.

#### Features:
- **Dynamic Property Editors**: Generates appropriate editors for each property type
- **Inline Editing**: Edit properties without leaving the visual interface
- **Validation**: Real-time validation of property values
- **Auto-save**: Automatic saving of property changes

## Communication Architecture

### PostMessage API

The block editor communicates with the parent Umbraco interface using PostMessage:

#### Outgoing Messages (to parent):
```typescript
// Block property editing
{
  messageType: 'blockfarmeditor:block-properties',
  action: 'edit',
  uniquePath: string,
  block: BlockDefinition,
  index?: number
}

// Save page changes
{
  messageType: 'blockfarmeditor:save',
  data: PageDefinition
}
```

#### Incoming Messages (from parent):
```typescript
// Initialize editor
{
  messageType: 'blockfarmeditor:initialize',
  data: PageDefinition,
  culture: string
}

// Show/hide layers
{
  messageType: 'blockfarmeditor:show-layers'
}
```

### Event System

Internal event system for component communication:

- `block-farm-editor`: Central event for all block operations
- `block-update`: Triggered when blocks are modified
- `blockfarmeditor:refresh-layers`: Updates layer panel
- `blockfarmeditor:initialized`: Editor initialization complete

## Global Variables

The editor relies on several global variables set by the parent application:

- `window.blockFarmEditor`: Main editor instance with page data
- `window.blockFarmEditorBasePath`: Base URL for API calls
- `window.blockFarmEditorUnique`: Unique content identifier
- `window.blockFarmEditor.culture`: Current editing culture

## API Integration

The editor communicates with the following server endpoints:

- `POST /umbraco/blockfarmeditor/renderblock/{contentId}` - Render individual blocks
- Various endpoints through the parent window for:
  - Block definitions
  - Property editing
  - Page saving

## Development

### Prerequisites

- Node.js and npm
- TypeScript knowledge
- Familiarity with Lit framework
- Understanding of web components and PostMessage communication

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

The component builds to `../wwwroot/block-editor/dist/front-end-elements.js` as a single ES module.

## Styling

The editor uses custom CSS with:

- **Block Overlays**: Semi-transparent overlays for editing interface
- **Drag & Drop Indicators**: Visual feedback during drag operations
- **Action Buttons**: Consistent styling for block actions
- **Responsive Design**: Adapts to different screen sizes
- **Layer Panel**: Styled hierarchical tree view
- **Focus States**: Clear visual focus indicators

## Block Lifecycle

### Block Addition
1. User clicks "Add Block" interface
2. Block catalog opens via PostMessage to parent
3. Selected block is added to page data
4. Block HTML is fetched from server
5. Block component is rendered with editing overlay
6. Page structure is updated

### Block Editing
1. User clicks edit action on block
2. Property editor opens via PostMessage to parent
3. Property changes are applied to block data
4. Block HTML is re-rendered with new data
5. Visual updates appear immediately

### Block Deletion
1. User clicks delete action on block
2. Block is removed from page data structure
3. DOM is updated to remove block element
4. Parent blocks update their child references
5. Layer panel refreshes

### Block Movement
1. User initiates drag operation on block
2. Drag indicators appear throughout page
3. Drop zones highlight valid placement areas
4. On drop, block data structure is updated
5. DOM is reordered to match new structure

## Error Handling

Comprehensive error handling includes:

- **Render Errors**: Graceful handling of block rendering failures
- **Network Errors**: Retry logic for API communication failures
- **PostMessage Errors**: Timeout and error detection for parent communication
- **DOM Errors**: Safe DOM manipulation with error recovery
- **State Errors**: Validation and recovery from invalid page states

## Performance Optimizations

- **Efficient Rendering**: Minimal DOM updates during block operations
- **Event Delegation**: Optimized event handling for large pages
- **Lazy Loading**: Components load only when needed
- **Debounced Updates**: Throttled updates during rapid changes
- **Memory Management**: Proper cleanup of event listeners and observers

## Browser Compatibility

- **Modern Browsers**: Supports all evergreen browsers
- **Web Components**: Uses native web component APIs
- **ES Modules**: Built as ES modules for optimal loading
- **PostMessage**: Secure cross-frame communication

## Dependencies

### Runtime Dependencies
- `lit`: ^3.3.0 - Web component framework

### Development Dependencies
- `typescript`: ~5.8.3 - TypeScript compiler
- `vite`: ^6.3.5 - Build tool and development server

## Integration

### Iframe Integration
The block editor runs within an iframe in Umbraco's preview mode:
- Isolated execution environment
- Secure PostMessage communication
- Full page editing capabilities
- Preview mode integration

### Umbraco Integration
- Works with Umbraco's preview service
- Integrates with document types and content types
- Respects Umbraco's security and permissions
- Uses Umbraco's property editor system

## Troubleshooting

### Common Issues
- **Blocks not rendering**: Check API endpoint accessibility and authentication
- **PostMessage errors**: Verify parent window origin and message format
- **Drag & drop not working**: Check event listeners and DOM structure
- **Property editing issues**: Verify property editor registrations in parent
- **Layer panel not updating**: Check event propagation and layer refresh events

### Debug Mode
Enable debug mode by setting `window.blockFarmEditorDebug = true` for additional logging.

## License

This component is part of the Block Farm Editor package. For support or issues, contact [https://blockfarmeditor.com/about/contactus](https://blockfarmeditor.com/about/contactus).
