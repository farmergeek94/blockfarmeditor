import "./components/BlockActions.ts"
import "./helpers/GlobalListeners.ts"
import "./components/Block.ts"
import "./components/AddBlock.ts"
import "./components/BlockProperties.ts"
import "./components/BlockLayers.ts"
import "./components/BlockLayerItem.ts"
import "./index.css"

import { html } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { repeat } from 'lit/directives/repeat.js';


import { BlockDefinition } from '../../models/BlockDefinition'
import { PageDefinition } from '../../models/PageDefinition'
import { type IBuilderProperties } from '../../models/IBuilderProperties'
import { GenerateGuid } from '../../helpers/GuidHelper'
import { type RenderedBlock } from './models/RenderedBlock'
import { BlockElement } from './components/BlockElement';
import { sendPropertiesMessage } from "./helpers/Actions.ts"
/**
 * An example element.
 *
 * @slot - This element has a slot
 * @csspart button - The button
 */
@customElement('block-area')
export class BlockArea extends BlockElement {
    constructor() {
        super()
        this.blockFarmEditorEventHandler = this.blockFarmEditorEventHandler.bind(this)
    }

    @state()
    private block: BlockDefinition<IBuilderProperties> | null = null

    @state()
    private blockHtml: RenderedBlock[] = []

    private retrieveBlockHtml = async (block: BlockDefinition<IBuilderProperties>) => {
        const html = await fetch(`${window.blockFarmEditorBasePath}/umbraco/blockfarmeditor/renderblock/${window.blockFarmEditorUnique}?culture=${window.blockFarmEditor.culture ?? ''}`, {
            method: 'POST',
            body: JSON.stringify({
                contentTypeKey: block.contentTypeKey,
                properties: block.properties,
                unique: block.unique,
                blocks: []
            }),
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        })
        if (html.ok) {
            return {
                unique: block.unique,
                contentTypeKey: block.contentTypeKey,
                html: await html.text(),
                hasProperties: !(!(block.properties && Object.keys(block.properties).length > 0)),
            }
        }
    }

    private blockFarmEditorLoad = async () => {
        try {
            this.block = window.blockFarmEditor.getBlock(this.uniquePath, true)
            for (let i = 0; i < (this.block?.blocks || []).length; i++) {
                const block = this.block?.blocks[i];
                if (!block) continue;
                const blockHtml = await this.retrieveBlockHtml(block)
                if (blockHtml) {
                    this.blockHtml.push(blockHtml)
                }
            }
            this.sortBlocks();
            window.dispatchEvent(new Event("blockfarmeditor:refresh-layers"));
        } catch (error) {
            window.blockFarmEditor.displayMessage('danger', 'Error loading block area. See console for details.', 'Block Farm Editor');
            console.error('Error loading block area for:', this.uniquePath, " message:", error)
        }
    }

    connectedCallback(): void {
        super.connectedCallback()
        window.addEventListener('block-farm-editor', this.blockFarmEditorEventHandler);
        window.addEventListener('message', this.windowMessageHandler);
        if (!window.blockFarmEditor) {
            window.addEventListener('blockfarmeditor:initialized', this.blockFarmEditorLoad)
        } else {
            this.blockFarmEditorLoad();
        }
        window.addEventListener('blockfarmeditor:refresh-areas', this.blockFarmEditorLoad);
        this.addEventListener('mouseleave', this.handleMouseLeave);
    }

    disconnectedCallback() {
        window.removeEventListener('blockfarmeditor:initialized', this.blockFarmEditorLoad);
        window.removeEventListener('message', this.windowMessageHandler);
        window.removeEventListener('blockfarmeditor:refresh-areas', this.blockFarmEditorLoad);
        this.removeEventListener('mouseleave', this.handleMouseLeave);
    }

    private windowMessageHandler = (event: MessageEvent) => {
        // Check if the message is from the same origin
        if (event.origin !== window.location.origin) return;

        if (event.data.messageType == 'blockfarmeditor:block-added') {
            if (event.data.uniquePath !== this.uniquePath) return;
            // retrieve the block from the block farm editor
            const block = window.blockFarmEditor.getBlock(event.data.uniquePath)
            if (block) {
                const newBlock = { 
                    contentTypeKey: event.data.block.contentTypeKey, 
                    unique: GenerateGuid(), 
                    blocks: [], 
                    properties: event.data.block.properties || {} 
                } as BlockDefinition<IBuilderProperties>;
                sendPropertiesMessage('add', newBlock, this.uniquePath + "/" + newBlock.unique, event.data.index);
            }
        } else if (event.data.messageType == 'blockfarmeditor:properties-updated') {
            if (event.data.blockUniquePath !== this.uniquePath) return;
            const blockArea = window.blockFarmEditor.getBlock(event.data.blockUniquePath)
            if(blockArea && event.data.action === 'add'){  
                // if we have an index, insert the block at that index
                // otherwise, insert it at the beginning
                if (event.data.index !== undefined && event.data.index >= 0) {
                    blockArea.blocks.splice(event.data.index, 0, event.data.block);
                } else {
                    blockArea.blocks.push(event.data.block);
                }
            }
            const block = window.blockFarmEditor.getBlock(event.data.uniquePath)
            if (block) {
                block.properties = event.data.block.properties;
                
                this.retrieveBlockHtml(block).then((html) => {
                    if (html) {
                        const existingBlock = this.blockHtml.find((b) => b.unique === html.unique);
                        if (existingBlock) {
                            this.blockHtml = this.blockHtml.map((b) => {
                                if (b.unique === existingBlock.unique) {
                                    return html;
                                }
                                return b;
                            });
                        } else {
                            this.blockHtml.push(html);
                            this.sortBlocks();
                        }
                    }
                    window.dispatchEvent(new Event("blockfarmeditor:refresh-layers"));
                });
            }
        } else if( event.data.messageType == 'blockfarmeditor:use-layout-selected') {
            if (event.data.uniquePath !== this.uniquePath) return;
            var block = window.blockFarmEditor.getBlock(this.uniquePath, true);
            if(!block) return;
            // Generate a new unique for the new block that way it doesn't conflict
            let newBlock = event.data.block;
            if(event.data.index !== undefined && event.data.index >= 0) {
                block.blocks.splice(event.data.index, 0, newBlock);
            } else {
                block.blocks.push(newBlock);
            }
            
            this.blockFarmEditorLoad();
        }
    }

    private addBlock() {
        window.parent.postMessage({
            messageType: 'blockfarmeditor:add-block',
            uniquePath: this.uniquePath,
            allowedblocks: this.allowedblocks
        }, window.location.origin)
    }

    private useLayout() {
        window.parent.postMessage({
            messageType: 'blockfarmeditor:use-layout',
            uniquePath: this.uniquePath
        }, window.location.origin)
    }

    private refreshBlock(e: CustomEvent) {
        const block = window.blockFarmEditor.getBlock(e.detail)
        if (block) {
            this.retrieveBlockHtml(block).then((html) => {
                if (html) {
                    this.blockHtml = this.blockHtml.map((b) => {
                        if (b.unique === html.unique) {
                            return html;
                        }
                        return b;
                    });
                }     
                window.dispatchEvent(new Event("blockfarmeditor:refresh-layers"));
            });
        }
    }

    private sortBlocks() {
        const blockArea = window.blockFarmEditor.getBlock(this.uniquePath)
        if (blockArea && blockArea.blocks) {
            return this.blockHtml = blockArea.blocks.map((block) => {
                const blockHtml = this.blockHtml.find((b) => b.unique === block.unique);
                if (blockHtml) {
                    return blockHtml;
                }
            }).filter((b) => b !== undefined);
        }
        return [];
    }

    private blockFarmEditorEventHandler = (ce: Event) => {
        const e = ce as CustomEvent;
        switch (e.detail.type) {
            case "block-delete":
                this.blockDelete(e);
                break;
            case "block-moved":
                this.blockMoved(e);
                break;
            case "block-move-start":
                this.handleBlockMoveStart(e);
                 break;
            case "block-save-layout":
                this.handleBlockSaveLayout(e);
                break;
        }
    }

    private blockDelete(e: Event) {
        const customEvent = e as CustomEvent;
        if(customEvent.detail && customEvent.detail.parentPath == this.uniquePath) {
            const block = window.blockFarmEditor.getBlock(customEvent.detail.uniquePath)
            if (block) {
                const parentBlock = window.blockFarmEditor.getBlock(this.uniquePath)
                if (parentBlock) {
                    parentBlock.blocks = parentBlock.blocks.filter((b) => b.unique !== block.unique)
                    this.blockHtml = this.blockHtml.filter((b) => b.unique !== block.unique)
                }
            }
            
            e.stopPropagation();
            e.preventDefault();
            window.dispatchEvent(new Event("blockfarmeditor:refresh-layers"));
        }
    }

    private handleBlockSaveLayout(e: Event) {
        const customEvent = e as CustomEvent;
        if(customEvent.detail && customEvent.detail.parentPath == this.uniquePath) {
            const block = window.blockFarmEditor.getBlock(customEvent.detail.uniquePath)

            const layout = JSON.stringify(block);

            window.parent.postMessage({
                messageType: 'blockfarmeditor:save-layout',
                uniquePath: customEvent.detail.uniquePath,
                parentPath: customEvent.detail.parentPath,
                layout: layout
            }, window.location.origin)
            
            e.stopPropagation();
            e.preventDefault();
        }
    }

    private blockMoved(e: Event) {
        const customEvent = e as CustomEvent
        if (customEvent.detail.source === this.uniquePath) {
            this.sortBlocks();
            window.dispatchEvent(new Event("blockfarmeditor:refresh-layers"));
        }
    }

    // Handle drag and drop events across block areas
    // This function is called when the user starts dragging a block
    private handleBlockMoveStart(evt: Event) {
        const e = evt as CustomEvent;
        if(e.detail && e.detail.parentPath == this.uniquePath) {
            window.blockFarmEditorDrag = {
                source: this.uniquePath,
                sourceIndex: e.detail.index,
                blockHtml: this.blockHtml,
                //allowedblocks: this.allowedblocks
            }

            // Add dragging class to the current block for styling
            const blockElement = this.querySelector(`.bfe--block-area-block:nth-child(${e.detail.index + 1})`) as HTMLElement;
            if (blockElement) {
                blockElement.classList.add('bfe--dragging');
            }

            // Prevent default to allow drag operation
            e.preventDefault();
            e.stopPropagation();
        }
    }

    private getAllowedBlocks(): Promise<string[]> {
        return fetch(window.blockFarmEditorBasePath + '/umbraco/blockfarmeditor/getblockdefinitions/',{
                credentials: 'include',
                body: JSON.stringify({allowedBlocks: this.allowedblocks}),
                headers: {
                    'Content-Type': 'application/json',
                },
                method: 'POST',
            }).then((response) => {
                if (response.ok) {
                    return response.json()
                } else {
                    throw new Error('Failed to fetch block definitions:' + response.statusText);
                }
            }).then((data: {Containers: BlockDefinition<IBuilderProperties>[], Blocks: BlockDefinition<IBuilderProperties>[]}) => {
                
                return [...(data.Blocks ?? []), ...(data.Containers ?? [])].map(def => def.contentTypeKey).filter(key => key !== undefined) as string[];
            });
    }

    // This function is called when the user releases the mouse button after dragging
    // It handles the logic of moving the block to a new position
    // It checks if the block is being moved to a different area and updates the block structure accordingly
    // It also removes the dragging class from all blocks
    // and resets the drag state
    public async handleBlockMoveEnd(e: MouseEvent, index: number) {
        if (!window.blockFarmEditorDrag || window.blockFarmEditorDrag.source === null) return;
        e.preventDefault();
        e.stopPropagation();

        const drag = window.blockFarmEditorDrag;
        let valid = drag.source !== null && drag.sourceIndex !== null && index !== null
            && (drag.source !== this.uniquePath || drag.sourceIndex !== index);

        const target = this.renderRoot as HTMLElement | null;
        try {
            if (target?.closest('.bfe--dragging')) {
                window.blockFarmEditor.displayMessage('warning', 'Cannot drop a block onto itself.', 'Block Farm Editor');
                valid = false;
            } 
        } catch (error) {
            console.error('Error checking drop target:', error);
            window.blockFarmEditor.displayMessage('danger', 'Error checking drop target.', 'Block Farm Editor');
            valid = false;
        }

        const offsetY = e.offsetY;
        const elementHeight = (e.target as HTMLElement)?.offsetHeight ?? 0;

        let finalIndex = index;

        // Only proceed if we have valid source and target indices
        if (valid) {

            // Move the block in the parent block's blocks array
            const sourceBlockArea = window.blockFarmEditor.getBlock(drag.source);
            const targetBlockArea = window.blockFarmEditor.getBlock(this.uniquePath);

            if (sourceBlockArea && sourceBlockArea.blocks) {

                const movingBlock = sourceBlockArea.blocks[drag.sourceIndex];
                
                const allowedBlocks = await this.getAllowedBlocks();
                if (allowedBlocks.includes(movingBlock?.contentTypeKey ?? "")) {
                    // Remove the block from source index
                    const [movedBlock] = sourceBlockArea.blocks.splice(drag.sourceIndex, 1);
                    // Add the block to the target index
                    if (targetBlockArea) {
                        if (!targetBlockArea.blocks) {
                            targetBlockArea.blocks = [];
                        }

                        finalIndex = index;
                        // If the target index is greater than the source index,
                        // we need to adjust the index to account for the removed block
                        // if the target area blocks are empty, we can just add it to the end
                        console.log("offsetY", offsetY, "elementHeight", elementHeight, targetBlockArea.blocks.length);
                        if (finalIndex !== null && offsetY > (elementHeight / 2) && targetBlockArea.blocks.length > 0) {
                            finalIndex += 1;
                        }
                        if (drag.source === this.uniquePath) {
                            // If the source and target are the same, 
                            // check to see if the index is greater than the source index
                            // If so, decrement the index to account for the removed block
                            if (finalIndex > drag.sourceIndex) {
                                finalIndex -= 1;
                            }
                        }

                        targetBlockArea.blocks.splice(finalIndex, 0, movedBlock);

                        if (drag.source !== this.uniquePath) {
                            this.blockHtml.push(drag.blockHtml[drag.sourceIndex])
                        }
                        // Update blockHtml array to match
                        this.sortBlocks();
                    }
                    
                    window.dispatchEvent(new CustomEvent('block-farm-editor', {
                        detail: {
                            type: "block-moved",
                            source: drag.source,
                            sourceIndex: drag.sourceIndex,
                            target: this.uniquePath,
                            targetIndex: finalIndex
                        }
                    }));
                } else {
                    window.blockFarmEditor.displayMessage('warning', `The block type "${window.blockFarmEditorDefinitions[movingBlock?.contentTypeKey ?? ""]}" cannot be added to ${window.blockFarmEditorDefinitions[targetBlockArea?.contentTypeKey ?? ""]}.`, 'Block Farm Editor');
                }
            }
        }

        // Remove dragging class
        const blockElements = document.body.querySelectorAll('.bfe--dragging, .drag-over') || [];
        blockElements?.forEach(el => {
            el.classList.remove('bfe--dragging', 'drag-over');
        });

        // Reset drag state
        window.blockFarmEditorDrag = null;
        this.onMouseLeave();

        document.documentElement.dispatchEvent(new Event('block-drag-end'));
    }

    // This function is called when the mouse hovers over a block
    // It adds a visual indicator to show where the block will be dropped
    // It also handles the case where the block is being dragged from another area
    // It adds a class to the block being hovered over to indicate the drop target
    // and removes the class from all other blocks
    private handleMouseOver(e: MouseEvent, index: number) {
        this.onMouseLeave();
        // use the block count to show the parent options if the block area is empty
        (this.block?.blocks.length ?? 0) != 0 && e.stopPropagation();
        const currentElement = e.currentTarget as HTMLElement;
        const rect = currentElement.getBoundingClientRect();
        const offsetY = e.clientY - rect.top; // Y offset relative to the current element
        const elementHeight = currentElement.offsetHeight;

        let classname = "top";
        if (offsetY > (elementHeight / 2)) {
            classname = "bottom";
        }

        // Get only direct children block elements of THIS block area
        const blockElements = this.querySelectorAll(':scope > .bfe--block-area-block');
        
        if (window.blockFarmEditorDrag && window.blockFarmEditorDrag.source !== null) {

            document.documentElement.dispatchEvent(new CustomEvent('block-drag-move', {
                detail: {
                    clientY: e.clientY,
                }
            }));

            // Add visual indicator for drag target
            const elems = blockElements || [];
            for (let i = 0; i < elems.length; i++) {
                if (i === index) {
                    elems[i].classList.add(`bfe--drag-over-${classname}`);
                    break;
                }
            }
        } else {
            // Add visual indicator for hover state
            const elems = blockElements || [];
            for (let i = 0; i < elems.length; i++) {
                if (i === index) {
                    elems[i].classList.add(`bfe--show-hover-${classname}`);
                    break;
                }
            }
        }
    }

    private handleMouseLeave() {
        this.onMouseLeave();
    }

    private onMouseLeave() {
        // Remove visual indicator for drag target
        const blockElements = document.body.querySelectorAll('.bfe--block-area-block');
        if (blockElements) {
            for (const el of blockElements) {
                el.classList.remove('bfe--drag-over-top', 'bfe--drag-over-bottom', 'bfe--show-hover-top', 'bfe--show-hover-bottom');
            }
        }
    }

    private renderEmptyBlock() {
        return html`
        <div class="bfe--block-area-block bfe--empty"
            @mouseup=${(e: MouseEvent) => this.handleBlockMoveEnd(e, 0)}
            @mouseover=${(e: MouseEvent) => this.handleMouseOver(e, 0)}>
            <add-block ?empty=${true} @add-block=${() => this.addBlock()} @use-layout=${() => this.useLayout()}>
            </add-block>
        </div>`
    }

    render() {
        const lastBlockIndex = this.blockHtml.length - 1;
        return html`
            ${repeat(this.blockHtml, (item) => item.unique, (item, index) => html`
                <div class="bfe--block-area-block"
                @mouseup=${(e: MouseEvent) => this.handleBlockMoveEnd(e, index)}
                @mouseover=${(e: MouseEvent) => this.handleMouseOver(e, index)}>
                    <block-item 
                    unique="${item.unique}" 
                    .parentUniquePath="${this.uniquePath}"
                    .contentTypeKey="${item.contentTypeKey}"
                    .index="${index}"
                    ?lastblock="${index === lastBlockIndex}"
                    ?hasproperties="${item.hasProperties}"
                    @block-update=${this.refreshBlock}
                    .html="${item.html}"
                    .allowedblocks=${this.allowedblocks}></block-item>
                </div>
            `)}
            ${(this.block?.blocks.length ?? 0) == 0 ? this.renderEmptyBlock() : ''}
        `
    }
}



declare global {
    interface HTMLElementTagNameMap {
        'block-area': BlockArea
    }

    interface Window {
        blockFarmEditor: {
            pageDefinition: PageDefinition
            culture?: string
            getBlock: (uniquePath: string, create?: boolean) => BlockDefinition<IBuilderProperties> | null
            getBlockIndex: (uniquePath: string) => number | undefined
            updateBlock: (uniquePath: string, updates: BlockDefinition<IBuilderProperties>) => boolean
            displayMessage: (color: "info" | "success" | "warning" | "danger", message: string, headline?: string | undefined) => void
        }
        blockFarmEditorUnique?: string
        blockFarmEditorBasePath?: string
        blockFarmEditorDrag?: {
            source: string
            sourceIndex: number
            blockHtml: RenderedBlock[]
        } | null
        blockFarmEditorDefinitions: Record<string, string>
    }
}