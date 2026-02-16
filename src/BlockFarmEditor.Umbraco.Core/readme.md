
# Block Farm Editor - Umbraco Core

This project exists to provide the shared, server-side foundation for Block Farm Editor across the Umbraco solution. It is the single place for the core contracts, data models, and persistence setup that every other Block Farm Editor project relies on.

## Why it exists

Block Farm Editor spans multiple projects (Umbraco integration, UI bundles, uSync, etc). Without a common core library, shared types and database logic would be duplicated and drift over time. This project keeps that shared behavior centralized so feature work can move quickly without breaking integration points.

## What lives here

- **Contracts and services** used by the Umbraco integration layer (interfaces such as `IBlockFarmEditorDefinitionService`, `IBlockFarmEditorLayoutService`, and rendering/context interfaces).
- **Data transfer objects (DTOs)** that define the database shape for definitions and layouts.
- **Database migrations** that create and maintain the Block Farm Editor tables.
- **Core models** used by both server-side rendering and the client-side builders (page and block definition models).
- **Attributes** that annotate block definitions and configuration.

## What does not live here

- Umbraco UI, backoffice extensions, or web components.
- Client-side build assets.

## Who depends on it

- The Umbraco integration project for API endpoints and runtime behavior.
- uSync and package projects for schema and configuration consistency.
- Any server-side rendering or definition management features.

## In short

This is the durable, shared core that keeps Block Farm Editor consistent across the solution, both at compile time (types/contracts) and at runtime (database shape and shared models).
