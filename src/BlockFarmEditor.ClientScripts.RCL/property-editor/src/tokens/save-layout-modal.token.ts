import { UmbModalToken } from '@umbraco-cms/backoffice/modal';

export interface SaveLayoutModalData {
    uniquePath?: string,
    parentPath?: string,
    layout: string,
    fullLayout?: boolean
}

export const BLOCKFARMEDITOR_SAVE_LAYOUT_MODAL = new UmbModalToken<SaveLayoutModalData>('BlockFarmEditor.SaveLayout.Modal', {
    modal: {
        type: 'sidebar',
        size: 'medium',
    }
});