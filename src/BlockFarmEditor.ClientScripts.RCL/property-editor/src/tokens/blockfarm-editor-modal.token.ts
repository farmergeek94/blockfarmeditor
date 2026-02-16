import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { PageDefinition } from '../../../models/PageDefinition';

export interface BlockFarmEditorModalData {
    value?: PageDefinition;
    contentUnique?: string;
    culture?: string;
}

export interface BlockFarmEditorModalResult extends PageDefinition {
}

export const BLOCKFARMEDITOR_MODAL = new UmbModalToken<BlockFarmEditorModalData, BlockFarmEditorModalResult>('BlockFarmEditor.Modal', {
    modal: {
        type: 'sidebar',
        size: 'full'
    }
});
