# Settings Dashboard

A Lit-based web component for managing Block Farm Editor licenses in the Umbraco backoffice.

## Overview

This settings dashboard provides a user interface for license management within the Umbraco CMS. It allows users to view, validate, and manage domain licenses for the Block Farm Editor package.

## Project Structure

```
settings-dashboard/
├── src/
│   ├── settings-dashboard.ts    # Main component implementation
│   ├── index.css               # Global styles
│   ├── vite-env.d.ts          # Vite type definitions
│   └── assets/
│       └── lit.svg            # Lit framework logo
├── public/
│   └── vite.svg               # Vite logo
├── package.json               # Dependencies and scripts
├── tsconfig.json             # TypeScript configuration
├── vite.config.ts            # Vite build configuration
├── index.html                # Development HTML template
└── .gitignore                # Git ignore rules
```

## Development

### Prerequisites

- Node.js and npm
- TypeScript knowledge
- Familiarity with Lit framework
- Umbraco CMS development environment

### Installation

```bash
npm install
```

### Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production (TypeScript compilation + Vite build)
- `npm run preview` - Preview production build

### Building

The component builds to `../wwwroot/settings-dashboard/dist/` as configured in the Vite config. This integrates with the .NET project structure.

## Component API

### Custom Element

The component is registered as `blockfarmeditor-settings-dashboard` and can be used in Umbraco's backoffice.

### Properties

- `licenses`: Array of license objects containing domain, license key, and expiration date
- `domain`: Currently selected domain
- `domainList`: Available domains for selection
- `message`: Status messages for user feedback
- `revalidating`: Loading state during license validation

### Methods

- `revalidateDomainLicense()`: Validates or revalidates the current domain's license
- `#getCurrentLicenses()`: Fetches existing licenses from the server
- `#getServerDomain()`: Extracts domain from current URL
- `#getBaseUrl()`: Gets the base URL for API calls

## API Integration

The component communicates with the following endpoints:

- `POST /umbraco/blockfarmeditor/getlicenses` - Retrieve existing licenses
- `POST /umbraco/blockfarmeditor/validatelicense?domain={domain}` - Validate domain license

## Styling

The component uses Umbraco's UI library (`uui-*` components) and includes:

- Responsive layout with proper spacing
- Form controls (inputs, selects, textareas)
- Loading indicators
- Message display areas

## Integration

This component is integrated into Umbraco as a settings dashboard through the package configuration in `wwwroot/umbraco-package.json`.

## Dependencies

### Runtime Dependencies
- `lit`: ^3.3.0 - Web component framework

### Development Dependencies
- `@umbraco-cms/backoffice`: ^16.0.0 - Umbraco backoffice types
- `typescript`: ~5.8.3 - TypeScript compiler
- `vite`: ^6.3.5 - Build tool

## License

This component is part of the Block Farm Editor package. For support or issues, contact [https://blockfarmeditor.com/about/contactus](https://blockfarmeditor.com/about/contactus).