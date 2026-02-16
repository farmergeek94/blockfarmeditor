import { css, html, LitElement } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import type { IBuilderProperties } from "../../../models/IBuilderProperties";
import type { BlockDefinition } from "../../../models/BlockDefinition";
import { repeat } from "lit/directives/repeat.js";
import type { BlockArea } from "../front-end-elements";

@customElement('block-layer-item')
export class BlockLayerItem extends LitElement { 
    constructor() {
        super();
        this.refreshLayout = this.refreshLayout.bind(this);
    }
    
    connectedCallback() {
        window.addEventListener('blockfarmeditor:refresh-items', this.refreshLayout);
        super.connectedCallback();
    }

    disconnectedCallback() {
        window.removeEventListener('blockfarmeditor:refresh-items', this.refreshLayout);
        super.disconnectedCallback();
    }
    
    @property({type: String})
    public uniquePath: string = '';

    private _item?: BlockDefinition<IBuilderProperties>;

    @property({type: Object})
    get item(): BlockDefinition<IBuilderProperties> | undefined {
        return this._item;
    }

    set item(value: BlockDefinition<IBuilderProperties> | undefined) {
        const oldValue = this._item;
        this._item = value;
        this.type = value?.contentTypeKey ? "block-item" : "block-area";
        this.requestUpdate('item', oldValue);
    }

    private _index?: number;

    @property({type: Number})
    get index(): number | undefined {
        return this._index;
    }

    set index(value: number | undefined) {
        const oldValue = this._index;
        this._index = value;
        this.requestUpdate('index', oldValue);
    }

    @state()
    public name?: string;
    
    @state()
    private type: "block-area" | "block-item" = "block-item";

    private _refreshName() {
        this._setName(this.item, this.index);
    }

    private _setName(item?: BlockDefinition<IBuilderProperties>, index?: number) {
        item = item || this.item;
        index = index ?? this.index;
        this.name = window.blockFarmEditorDefinitions?.[item?.contentTypeKey ?? ''] ?? item?.contentTypeKey ?? (document.body.querySelector(`block-area[unique='${item?.unique}']`)?.getAttribute('identifier') || "block-area-" + ((index ?? 0) + 1));
    }

    private refreshLayout() {
        this._refreshName();
        this.requestUpdate();
    }

    handleMouseLeave(e: MouseEvent) {
        e.stopPropagation();
        const layerItem = this.shadowRoot?.querySelector('.layer-item');
        if (layerItem) {
            layerItem.classList.remove('drag-over-top', 'drag-over-bottom');
        }
    }

    // This function is called when the mouse hovers over a block
    // It adds a visual indicator to show where the block will be dropped
    // It also handles the case where the block is being dragged from another area
    // It adds a class to the block being hovered over to indicate the drop target
    // and removes the class from all other blocks
    private handleMouseOver(e: MouseEvent) {
        this.handleMouseLeave(e);
        if (window.blockFarmEditorDrag && window.blockFarmEditorDrag.source !== null){
            e.stopPropagation();
            const currentElement = e.currentTarget as HTMLElement;
            const rect = currentElement.getBoundingClientRect();
            const offsetY = e.clientY - rect.top; // Y offset relative to the current element
            const elementHeight = currentElement.offsetHeight;

            let classname = "top";
            if (offsetY > (elementHeight / 2)) {
                classname = "bottom";
            }

            // Get only direct children block elements of THIS block area
            const layerItem = this.shadowRoot?.querySelector('.layer-item');
            if (layerItem) {
                layerItem.classList.add("drag-over-" + classname);
            }
            this.dispatchEvent(new CustomEvent('block-drag-move', {
                detail: {
                    clientY: e.clientY,
                }
                , bubbles: true
                , composed: true
            }));
        }
    }

    querySelectorByLevel(root: Element, selector: string) {
        const queue = [root];

        while (queue.length > 0) {
            const nextLevel = [];

            for (const node of queue) {
                if (node.matches && node.matches(selector)) {
                    return node;
                }

                if (node.children) {
                    nextLevel.push(...node.children);
                }
            }

            queue.splice(0, queue.length, ...nextLevel); // Move to next level
        }

        return null; // not found
    }

    handleBlockMove(e: MouseEvent) {
        e.stopPropagation();
        const uniqueParts = this.uniquePath.split('/');
        let block: Element | null = null;
        let blockArea: BlockArea | null = null;

        // Find the block area element that matches the unique path
        for(const part of uniqueParts) {
            if (part) {
                let areaElement
                if (block) {
                    areaElement = this.querySelectorByLevel(block, `block-area[unique="${part}"], block-item[unique="${part}"]`);
                } else {
                    areaElement = this.querySelectorByLevel(document.body, `block-area[unique="${part}"]`);
                }
                if (areaElement) {
                    block = areaElement;
                }
                if (areaElement && areaElement.tagName.toLowerCase() === 'block-area') {
                    blockArea = areaElement as BlockArea;
                }
            }
        }
        if (blockArea) {
            // If the block area is found, handle the block move end
            blockArea.handleBlockMoveEnd(e, this.index ?? 0);
        }
    }

    render() {
        return html`
    <div class="layer-item ${this.type}"
        @mouseleave=${this.handleMouseLeave}
        @mouseover=${this.handleMouseOver}
        @mouseup=${this.handleBlockMove}>
        <div>${this.name}</div>${this.type === "block-item" ? html`
            <block-actions 
                .uniquePath=${this.uniquePath || ''}
                .hasProperties=${!(!(this.item?.properties && Object.keys(this.item.properties).length > 0))}
                .index=${this.index}
                ></block-actions>
            ` : ''}
    </div>
    ${this.item?.blocks && this.item.blocks.length > 0 ? html`<div class="child-menu">
        ${repeat(this.item.blocks, (item => item.unique), ((item, index) => {
            return html`
                <block-layer-item
                    .uniquePath=${this.uniquePath + '/' + item.unique}
                    .item=${item}
                    .index=${index}
                ></block-layer-item>          
            `
        }))}
    </div>
    `: ''}
        `
    }

    static styles = css`
        .child-menu {
            border-top: 1px dotted #ccc;
            padding-left: 10px;
        }
        .layer-item {
            display: flex;
            align-items: center;
            transition: all 0.2s ease;
            justify-content: space-between;
        }

        .layer-item.drag-over-top {
            border-top: 2px solid rgb(27, 38, 79);
            margin-top: -2px;
            cursor: move;
        }

        .layer-item.drag-over-bottom {
            border-bottom: 2px solid rgb(27, 38, 79);
            margin-bottom: -2px;
            cursor: move;
        }
        
        .layer-item.block-area {
            color: #888;
            font-style: italic;
            font-size: 11.2px !important;
            line-height: 11.2px !important;
        }
        .layer-item.block-item {
            padding-top: 4px !important;
            padding-bottom: 4px !important;
        }
        block-layer-item {
            border-bottom: 1px dotted #ccc;
        }

        block-layer-item:last-child {
            border-bottom: 1px dotted #ccc;
        }
    `
}


declare global {
    interface HTMLElementTagNameMap {
        'block-layer-item': BlockLayerItem
    }
}