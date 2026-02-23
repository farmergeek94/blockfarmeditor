import { UmbModalToken } from '@umbraco-cms/backoffice/modal';

export interface ExportModalData {}

export interface ExportDefinitionItem {
    key: string;
    name: string;
    description?: string;
    alias: string;
    category: string;
    enabled: boolean;
    selected?: boolean;
    icon?: string;
}

export interface ExportModalResult {
    selectedDefinitions: string[];  // GUID keys
    download: boolean;
}

export const BLOCKFARMEDITOR_EXPORT_MODAL = new UmbModalToken<ExportModalData, ExportModalResult>(
    'BlockFarmEditor.Export.Modal',
    {
        modal: {
            type: 'sidebar',
            size: 'medium'
        }
    }
);
