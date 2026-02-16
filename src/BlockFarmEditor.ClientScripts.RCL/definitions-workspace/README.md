# Definitions Workspace

A Lit-based Umbraco workspace extension for managing Block Farm Editor element definitions within the document type editing experience.

## Overview

This workspace provides an integrated interface for creating and managing Block Farm Editor definitions directly within Umbraco's document type workspace. It allows users to configure how document types are rendered as blocks in the Block Farm Editor, including view paths, categories, and rendering options.

## Features

- **Integrated Workspace**: Seamlessly integrated into Umbraco's document type editing workflow
- **Definition Management**: Create and update Block Farm Editor definitions for document types
- **View Type Support**: Support for both Partial Views and View Components
- **File Browser Integration**: Built-in partial view picker using Umbraco's file browser
- **Category Management**: Dynamic category selection with the ability to create new categories
- **Real-time Validation**: Form validation and error handling with user feedback
- **Auto-save Integration**: Integrates with Umbraco's workspace save events

## Project Structure

```
definitions-workspace/
├── src/
│   ├── definitions-workspace.ts    # Main workspace component
│   ├── element-condition.ts        # Element type condition logic
│   ├── vite-env.d.ts              # Vite type definitions
│   └── assets/
├── public/
│   └── vite.svg                   # Vite logo
├── package.json                   # Dependencies and scripts
├── tsconfig.json                  # TypeScript configuration
├── vite.config.ts                 # Vite build configuration
└── README.md                      # This file
```

## Components

### DefinitionsWorkspace (`definitions-workspace.ts`)

The main workspace component that provides the Block Farm Editor definition management interface.

#### Features:
- **Document Type Integration**: Automatically loads when editing element document types
- **Form Management**: Complete CRUD operations for block definitions
- **File Picker**: Integration with Umbraco's partial view picker modal
- **Category Management**: Dynamic category handling with new category creation
- **Validation**: Form validation and error handling
- **Auto-save**: Responds to workspace save events

#### Key Properties:
- `formData`: Block definition data structure
- `categories`: Available definition categories
- `newCategory`: Flag for creating new categories
- `isEditing`: Edit mode state
- `saving`: Save operation state

### Element Condition (`element-condition.ts`)

A condition extension that determines when the definitions workspace should be available.

#### Features:
- **Element Type Detection**: Shows only for element document types
- **Configurable Matching**: Supports both positive and negative matching
- **Context Awareness**: Integrates with document type workspace context

## API Integration

The workspace communicates with the following endpoints:

- `GET /umbraco/blockfarmeditor/definitions?alias={alias}` - Retrieve definition by content type alias
- `GET /umbraco/blockfarmeditor/definitions/categories` - Retrieve available categories
- `POST /umbraco/blockfarmeditor/definitions/create` - Create new definition
- `PUT /umbraco/blockfarmeditor/definitions/update/{id}` - Update existing definition

## Development

### Prerequisites

- Node.js and npm
- TypeScript knowledge
- Familiarity with Lit framework and Umbraco backoffice development
- Understanding of Umbraco's workspace and extension system

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

The component builds to `../wwwroot/definitions-workspace/dist/` with the following outputs:
- `definitions-workspace.js` - Main workspace component
- `element-condition.js` - Element condition logic

## Form Fields

### Content Type Alias
- **Type**: Text input (disabled)
- **Description**: The alias of the associated document type
- **Auto-populated**: Filled from workspace context

### Type
- **Type**: Select dropdown
- **Options**: 
  - `partial` - Partial View
  - `viewcomponent` - View Component
- **Required**: Yes
- **Description**: Determines the rendering mechanism

### View Path (Partial Views Only)
- **Type**: Text input with file picker
- **Format**: `~/Views/Partials/...`
- **Required**: Yes (for partial type)
- **File Picker**: Integrated Umbraco partial view picker

### Category
- **Type**: Select dropdown with new category option
- **Dynamic**: Categories loaded from server
- **New Category**: Allows creating new categories inline
- **Required**: Yes

### Enabled
- **Type**: Checkbox
- **Default**: True
- **Description**: Toggle to enable/disable the definition

## Styling

The component uses Umbraco's UI library (`uui-*` components) and custom CSS:

- **Responsive Layout**: Proper spacing and form organization
- **View Path Input**: Flex layout for input + button combination
- **Category Management**: Dynamic input for new categories
- **Form Sections**: Organized sections with visual separation

## Integration

This workspace is registered as an Umbraco workspace extension through the package configuration and appears as a tab/section within the document type editing interface when editing element types.

### Extension Registration

The workspace is registered with conditions that ensure it only appears for appropriate document types (elements).

### Context Integration

- **Document Type Workspace**: Observes document type data changes
- **Auth Context**: Handles authentication for API calls
- **Modal Manager**: Manages file picker modals
- **Notification Context**: Displays success/error messages
- **Action Event Context**: Responds to workspace save events

## Error Handling

The component includes comprehensive error handling:

- **API Errors**: Network and server errors are caught and displayed
- **Validation Errors**: Form validation with user feedback
- **Loading States**: Loading indicators during operations
- **Notifications**: Success and error notifications using Umbraco's notification system

## Dependencies

### Runtime Dependencies
- `@umbraco-cms/backoffice`: ^16.0.0 - Umbraco backoffice framework
- `lit`: ^3.3.0 - Web component framework

### Development Dependencies
- `typescript`: ~5.8.3 - TypeScript compiler
- `vite`: ^7.0.4 - Build tool and development server

## Usage

1. **Navigate** to a document type that is marked as an "Element"
2. **Access** the Block Farm Editor definitions workspace (appears as a tab/section)
3. **Configure** the definition by:
   - Selecting the rendering type (Partial/View Component)
   - Choosing or entering a view path (for partials)
   - Selecting or creating a category
   - Enabling/disabling the definition
4. **Save** the document type to persist the definition

## License

This component is part of the Block Farm Editor package. For support or issues, contact [https://blockfarmeditor.com/about/contactus](https://blockfarmeditor.com/about/contactus).
