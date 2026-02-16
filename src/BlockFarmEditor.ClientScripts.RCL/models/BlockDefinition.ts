import type { IBuilderProperties } from "./IBuilderProperties";
import type { IContainerDefinition } from "./IContainerDefinition";

/**
 * Block definition class with generic type parameter
 */
export class BlockDefinition<T extends IBuilderProperties> implements IContainerDefinition {
    /**
     * identifier for the block
     */
    public contentTypeKey?: string

    /**
     * unique identifer used by the system
     */
    public unique: string = "";

    /**
     * Properties for the block
     */
    public properties?: T;

    /**
     * Nested blocks within this block
     */
    public blocks: BlockDefinition<IBuilderProperties>[] = [];
}