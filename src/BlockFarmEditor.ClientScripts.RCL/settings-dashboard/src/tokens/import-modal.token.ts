import { UmbModalToken } from '@umbraco-cms/backoffice/modal';

export interface ImportModalData {}

export interface ImportModalResult {
    file?: File;
    overwriteElementTypes: boolean;
    overwriteCompositions: boolean;
    overwriteBlockDefinitions: boolean;
    overwritePartialViews: boolean;
    overwriteDataTypes: boolean;
}

export const BLOCKFARMEDITOR_IMPORT_MODAL = new UmbModalToken<ImportModalData, ImportModalResult>(
    'BlockFarmEditor.Import.Modal',
    {
        modal: {
            type: 'sidebar',
            size: 'small'
        }
    }
);
