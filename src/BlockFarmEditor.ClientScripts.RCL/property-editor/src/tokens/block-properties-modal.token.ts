import type { UmbPropertyValueData } from '@umbraco-cms/backoffice/property';
import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { BlockDefinition } from '../../../models/BlockDefinition';
import type { IBuilderProperties } from '../../../models/IBuilderProperties';

export interface BlockPropertiesModalData {
    block: BlockDefinition<IBuilderProperties>;
    index?: number;
    action: 'edit' | 'add';
    uniquePath: string;
}

export interface BlockPropertiesModalResult {
    properties: UmbPropertyValueData<any>[];
    block?: BlockDefinition<IBuilderProperties>;
    uniquePath?: string;
    index?: number;
    action: 'edit' | 'add';
}

export const BLOCKFARMEDITOR_PROPERTIES_MODAL = new UmbModalToken<BlockPropertiesModalData, BlockPropertiesModalResult>('BlockFarmEditor.Properties.Modal', {
    modal: {
        type: 'sidebar',
        size: 'large'
    }
});