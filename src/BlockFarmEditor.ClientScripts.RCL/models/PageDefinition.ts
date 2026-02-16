import type { BlockDefinition } from "./BlockDefinition";
import type { IBuilderProperties } from "./IBuilderProperties";
import type { IContainerDefinition } from "./IContainerDefinition";

/**
 * PageDefinition class that implements IContainerDefinition
 */

export const PageDefinitionUnique = "64D51B94-10C1-4225-9329-B2A54D0E47CE";

export class PageDefinition implements IContainerDefinition {
    public identifier: string = "BlockFarmEditor";
    public unique: string = PageDefinitionUnique;
    public blocks: BlockDefinition<IBuilderProperties>[] = [];
}