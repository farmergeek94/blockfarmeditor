import type { BlockDefinition } from "../../../models/BlockDefinition";
import type { IBuilderProperties } from "../../../models/IBuilderProperties";



export function sendPropertiesMessage(action: string, block: BlockDefinition<IBuilderProperties>, uniquePath: string, index?: number) {
        window.parent.postMessage({
            messageType: 'blockfarmeditor:block-properties',
            action,
            uniquePath,
            block: {...block, blocks: []},
            index
        }, window.location.origin)
    }