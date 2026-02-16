import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type BlockFarmEditorBlockDefinition from '../../../models/BlockFarmEditorBlockDefinition';

export interface AddBlockModalData {
    uniquePath: string;
    index?: number;
    allowedBlocks?: string;
}

export interface AddBlockModalResult {
    block?: BlockFarmEditorBlockDefinition;
    uniquePath: string;
    index?: number;
}

export const BLOCKFARMEDITOR_ADD_BLOCK_MODAL = new UmbModalToken<AddBlockModalData, AddBlockModalResult>('BlockFarmEditor.AddBlock.Modal', {
    modal: {
        type: 'sidebar',
        size: 'medium'
    }
});