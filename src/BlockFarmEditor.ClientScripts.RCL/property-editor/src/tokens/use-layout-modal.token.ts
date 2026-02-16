import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { IBuilderProperties } from '../../../models/IBuilderProperties';
import type { BlockDefinition } from '../../../models/BlockDefinition';
import type { PageDefinition } from '../../../models/PageDefinition';

export interface UseLayoutModalData {
    uniquePath: string;
    index?: number;
    fullLayout?: boolean;
}

export interface UseLayoutModalResult {
    block?: BlockDefinition<IBuilderProperties>;
    pageDefinition?: PageDefinition;
    uniquePath?: string;
    index?: number;
    fullLayout?: boolean;
}

export const BLOCKFARMEDITOR_USE_LAYOUT_MODAL = new UmbModalToken<UseLayoutModalData, UseLayoutModalResult>('BlockFarmEditor.UseLayout.Modal', {
    modal: {
        type: 'sidebar',
        size: 'medium'
    }
});