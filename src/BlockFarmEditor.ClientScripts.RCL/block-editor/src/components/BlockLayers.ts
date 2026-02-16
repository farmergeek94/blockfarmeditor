import { css, html, LitElement } from "lit";
import { customElement, state } from "lit/decorators.js";
import { repeat } from "lit/directives/repeat.js";
import { PageDefinitionUnique, type PageDefinition } from "../../../models/PageDefinition";


/**
 * An example element.
 *
 * @slot - This element has a slot
 * @csspart button - The button
 */
@customElement('block-layers')
export class BlockLayers extends LitElement { 
    constructor() {
        super();
        this._refreshPageDefinition = this._refreshPageDefinition.bind(this);
        this.windowMessageHandler = this.windowMessageHandler.bind(this);
        this._onMouseDown = this._onMouseDown.bind(this);
        this._onMouseMove = this._onMouseMove.bind(this);
        this._onMouseUp = this._onMouseUp.bind(this);
        this.handleDrag = this.handleDrag.bind(this);
        this.stopScrolling = this.stopScrolling.bind(this);
    }
    @state()
    private pageDefinition?: PageDefinition | null

    @state()
    private blockCount: number = 0;

    @state()
    private isDragging = false;

    private dragOffset = { x: 0, y: 0 };
    private position = { x: 0, y: 0 };
    private intervalId?: number;
    private scrollInterval: number | null = null;
    private readonly scrollDistance = 5;
    private readonly edgeThreshold = 50;

    connectedCallback() {
        super.connectedCallback();
        window.addEventListener('blockfarmeditor:refresh-layers', this._refreshPageDefinition);
        this.intervalId = setInterval(this._refreshPageDefinition, 5000); // Refresh every second
        window.addEventListener('message', this.windowMessageHandler);
        this.addEventListener('block-drag-move', this.handleDrag);
        document.documentElement.addEventListener('block-drag-end', this.stopScrolling);
    }

    disconnectedCallback() {
        window.removeEventListener('blockfarmeditor:refresh-layers', this._refreshPageDefinition);
        window.removeEventListener('message', this.windowMessageHandler);
        this.removeEventListener('block-drag-move', this.handleDrag);
        document.documentElement.removeEventListener('block-drag-end', this.stopScrolling);

        this.intervalId && clearInterval(this.intervalId);
        this.stopScrolling();
        super.disconnectedCallback();
    }

    private windowMessageHandler(event: MessageEvent) {
        console.log('Received message in block-layers:', event.data);
        if( event.data && event.data.messageType === 'blockfarmeditor:show-layers') {
            this.style.display = 'block';
            this.style.left = 'auto'; // Reset left to allow right positioning
            this.style.top = "80px";
            this.style.right = '50px'; // Set right to 50px
            this._refreshPageDefinition();
        } else if( event.data && event.data.messageType === 'blockfarmeditor:use-layout-selected') {
            console.log('Using layout from layers:', event.data);
            if(event.data.fullLayout) {
                window.blockFarmEditor.pageDefinition = event.data.pageDefinition;
                
                window.dispatchEvent(new Event('blockfarmeditor:refresh-areas'));
            }
        }
    }

    private _refreshPageDefinition () {
        if(window.blockFarmEditor?.pageDefinition && window.blockFarmEditor.pageDefinition.blocks){
            window.blockFarmEditor.pageDefinition.blocks = [...window.blockFarmEditor.pageDefinition.blocks];
        }
        this.pageDefinition = window.blockFarmEditor.pageDefinition;
        this.blockCount = this.pageDefinition?.blocks ? this.pageDefinition.blocks.reduce((a,x)=>a + x.blocks.length, 0) : 0;
        window.dispatchEvent(new Event('blockfarmeditor:refresh-items'));
    }

    private closeLayers() {
        this.style.display = 'none';
    }

    private _onMouseDown(e: MouseEvent) {
        if ((e.target as HTMLElement).closest('button')) return; // Don't drag when clicking close button
        
        this.isDragging = true;
        const rect = this.getBoundingClientRect();
        this.dragOffset.x = e.clientX - rect.left;
        this.dragOffset.y = e.clientY - rect.top;

        this.style.left = rect.left + 'px';
        this.style.top = rect.top + 'px';
        
        document.addEventListener('mousemove', this._onMouseMove);
        document.addEventListener('mouseup', this._onMouseUp);
        e.preventDefault();
    }

    private _onMouseMove(e: MouseEvent) {
        if (!this.isDragging) return;
        
        this.position.x = e.clientX - this.dragOffset.x;
        this.position.y = e.clientY - this.dragOffset.y;
        
        this.style.right = 'auto'; // Reset right to allow left positioning
        this.style.top = this.position.y + 'px';
        this.style.left = this.position.x + 'px';
    }

    private _onMouseUp() {
        this.isDragging = false;
        document.removeEventListener('mousemove', this._onMouseMove);
        document.removeEventListener('mouseup', this._onMouseUp);
    }

    // handle scrolling when dragging near the edges
    private startScrollingDown() {
        if (this.scrollInterval) return; // Prevent multiple intervals
        this.scrollInterval = setInterval(() => {
            const main = this.shadowRoot?.querySelector(".main");
            main && main.scrollBy(0, this.scrollDistance);
        }, 10);
    }

    private startScrollingUp() {
        if (this.scrollInterval) return; // Prevent multiple intervals
        this.scrollInterval = setInterval(() => {
            const main = this.shadowRoot?.querySelector(".main");
            main && main.scrollBy(0, -this.scrollDistance);
        }, 10);
    }

    private stopScrolling() {
        if (this.scrollInterval) {
            clearInterval(this.scrollInterval);
            this.scrollInterval = null;
        }
    }

    private handleDrag(event: Event) {
        const ce = event as CustomEvent;
        const main = this.shadowRoot?.querySelector(".main") as HTMLElement;
        if (!main) {
            return;
        }
        const viewportHeight = main?.clientHeight ?? 0;
        if(viewportHeight === 0) {
            return;
        }
        const rect = main.getBoundingClientRect();

        const elementY = ce.detail.clientY;

        // Check if near bottom edge for scrolling down
        if (viewportHeight - elementY < this.edgeThreshold) {
            this.stopScrolling(); // Stop any existing scroll
            this.startScrollingDown();
        }
        // Check if near top edge for scrolling up
        else if (elementY - rect.top - this.edgeThreshold < this.edgeThreshold) {
            this.stopScrolling(); // Stop any existing scroll
            this.startScrollingUp();
        }
        // Not near any edge, stop scrolling
        else {
            this.stopScrolling();
        }
        event.stopPropagation();
    }

    private useLayout(e: Event) {
         window.parent.postMessage({
            messageType: 'blockfarmeditor:use-layout',
            fullLayout: true
        }, window.location.origin);
        e.preventDefault();
        e.stopPropagation();
    }

    private saveLayout(e: Event) {
        const layout = JSON.stringify(this.pageDefinition);

            window.parent.postMessage({
                messageType: 'blockfarmeditor:save-layout',
                fullLayout: true,
                layout: layout
            }, window.location.origin);

            e.preventDefault();
            e.stopPropagation();
    }

    render() {
        const blocks = this.pageDefinition?.blocks;
        return html`
        <div class="header" @mousedown=${this._onMouseDown}>
            <span style="flex: 1">Editor Layers</span>
            <button @click=${this.closeLayers}>
                <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" class="bi bi-x" viewBox="0 0 16 16">
                    <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708"/>
                </svg>
            </button>
        </div>
        <div class="main">
            ${blocks && blocks.length > 0 ? html`
                ${repeat(blocks, (item => item.unique), ((item, index) => {
                    return html`
                        <block-layer-item
                            .uniquePath=${`${PageDefinitionUnique}/${item.unique}`}
                            .item=${item}
                            .index=${index}
                        ></block-layer-item>          
    `               }))}
            `: ''}
        </div>
         <div class="footer">
            ${this.blockCount == 0 ? html`
                <button @click=${this.useLayout} title="Use Layout">
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" class="bi bi-save" viewBox="0 0 16 16">
                        <path d="M2 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H9.5a1 1 0 0 0-1 1v7.293l2.646-2.647a.5.5 0 0 1 .708.708l-3.5 3.5a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L7.5 9.293V2a2 2 0 0 1 2-2H14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h2.5a.5.5 0 0 1 0 1z"/>
                    </svg>
                </button>
            ` : html`
                <button @click=${this.saveLayout} title="Save Layout">
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" class="bi bi-floppy" viewBox="0 0 16 16">
                        <path d="M11 2H9v3h2z"/>
                        <path d="M1.5 0h11.586a1.5 1.5 0 0 1 1.06.44l1.415 1.414A1.5 1.5 0 0 1 16 2.914V14.5a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 0 14.5v-13A1.5 1.5 0 0 1 1.5 0M1 1.5v13a.5.5 0 0 0 .5.5H2v-4.5A1.5 1.5 0 0 1 3.5 9h9a1.5 1.5 0 0 1 1.5 1.5V15h.5a.5.5 0 0 0 .5-.5V2.914a.5.5 0 0 0-.146-.353l-1.415-1.415A.5.5 0 0 0 13.086 1H13v4.5A1.5 1.5 0 0 1 11.5 7h-7A1.5 1.5 0 0 1 3 5.5V1H1.5a.5.5 0 0 0-.5.5m3 4a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5V1H4zM3 15h10v-4.5a.5.5 0 0 0-.5-.5h-9a.5.5 0 0 0-.5.5z"/>
                    </svg>
                </button>
            `}
        </div>
        `
    }

    static styles = css`
        :host {
            display: block;
            position: fixed;
            min-width: 200px;
            right: 50px;
            top: 80px;
            border: 1px solid #ccc;
            background: white;
            border-radius: 3px;
            padding-top: 0px;
            z-index: 9999;
        }

        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: move;
            user-select: none;
            padding: 4px 8px;
            background: rgb(27, 38, 79);
            color: white;
            gap: 4px;
        }

        .main {
            border-top: 1px solid #ccc;
            padding: 4px 8px;
            max-height: 70vh;
            max-width: 30vw;
            overflow: auto;

        }

        block-layer-item {
            border-bottom: 1px dotted #ccc;
        }

        block-layer-item:last-child {
            border-bottom: 1px dotted #ccc;
        }
        
        button {
            background: rgb(27, 38, 79);
            color: #333;
            border: none;
            cursor: pointer;
            font-size: 16px !important;
            line-height: 16px !important;
            padding: 0px !important;
            border-radius: 3px;
            flex: 0;
            color: white;
            display: flex;
            align-items: center;
            justify-content: flex-end;
        }

        button:hover {
            background: rgb(27, 38, 79);
        }

        button.block-button-delete:hover {
            background: rgb(255, 0, 0);
        }

        button:focus {
            outline: none;
        }

        .footer {
            display: flex;
            justify-content: flex-end;
            align-items: center;
            cursor: move;
            user-select: none;
            padding: 4px 8px;
            background: rgb(27, 38, 79);
            color: white;
            gap: 8px;
        }
    `
}


declare global {
    interface HTMLElementTagNameMap {
        'block-layers': BlockLayers
    }
}