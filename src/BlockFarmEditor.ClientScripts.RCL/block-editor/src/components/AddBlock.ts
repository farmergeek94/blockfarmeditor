import { LitElement, html } from "lit";
import { customElement, property, state } from 'lit/decorators.js'
import type { Block } from "./Block";

@customElement('add-block')
export class AddBlock extends LitElement {
    // Override the method to prevent creating a shadow DOM
    // This is important for the block element to work correctly
    protected createRenderRoot(): HTMLElement | DocumentFragment {
        return this;
    }

    @property({ type: Number })
    index?: number;

    @property({ type: Boolean })
    empty?: boolean;

    @property({ type: Boolean })
    lastblock?: boolean;

    @state()
    private _parentBlocks: Block[] = [];
    
    @state()
    private _open_menu: boolean = false;

    connectedCallback(): void {
        super.connectedCallback()
        this.closeMenu = this.closeMenu.bind(this);
        document.body.addEventListener('click', this.closeMenu);
        this.setParentBlocksWithProperties();
        this.setParentBlocksWithProperties = this.setParentBlocksWithProperties.bind(this);
        window.addEventListener('blockfarmeditor:refresh-layers', this.setParentBlocksWithProperties);
    }
    disconnectedCallback(): void {
        super.disconnectedCallback()
        document.body.removeEventListener('click', this.closeMenu);
        window.removeEventListener('blockfarmeditor:refresh-layers', this.setParentBlocksWithProperties);
    }

    private closeMenu = () => {
        this._open_menu = false;
    }

    private setParentBlocksWithProperties = () => {
        // Delay to ensure the DOM is fully updated before calculating parent blocks
        // Otherwise, we might get stale data
        setTimeout(() => {
            this.setParentBlocksWithPropertiesDelayed();
        }, 100);
    }

    private setParentBlocksWithPropertiesDelayed() {
        const parentBlocks: Block[] = [];
        const root = this.parentElement;
        const firstBlock = root?.closest('block-item') as Block | null;
        if (!firstBlock) {
            this._parentBlocks = parentBlocks;
            return;
        }

        // Start from the ancestor of the closest block-item
        let parentBlock = firstBlock.parentElement?.closest('block-item') as Block | null;
        const isZeroIndex = this.index === 0;

        while (parentBlock) {
            parentBlocks.push(parentBlock);

            // Break unless this parent allows continuing upward
            // logic: current block is not a first item and the parent is not a first or the last item break the loop.
            // this prevents showing block types from higher levels when adding a block in the middle of a list
            if (!parentBlock.lastblock && !(parentBlock.index === 0 && isZeroIndex)) {
                break;
            }

            parentBlock = parentBlock.parentElement?.closest('block-item') as Block | null;
        }
        this._parentBlocks = parentBlocks;
    }

    render() {
        return html`
            <!-- Add a hover area at the bottom of the block for adding new blocks -->
            <div class="bfe--block-area-add ${this.empty ? "bfe--empty" : ""}"
            @mouseover=${(e: MouseEvent) => !this.empty && e.stopPropagation()}>     
                <button class="bfe--button-add" @click=${() => { this.dispatchEvent(new CustomEvent("add-block", {
                    detail: { index: this.index },
                })) }}>
                    <div class="bfe--button-add-first-item">
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" class="bi bi-plus" viewBox="0 0 16 16">
                            <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4"/>
                        </svg> 
                        ${this.index == 0 || this.lastblock || this.empty ? html`      
                            <a href="#" class="bfe--open-menu" @click=${(e: MouseEvent) => { 
                                this._open_menu = !this._open_menu; 
                                e.stopPropagation();
                                e.preventDefault();
                            }}>
                                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="32" fill="currentColor" class="bi bi-caret-down-fill" viewBox="0 0 16 16">
                                    <path d="M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"/>
                                </svg>
                            </a>
                            ` : ''}
                    </div>
                    ${this.index == 0 || this.lastblock || this.empty ? html`   
                        ${this._open_menu ? html`
                            <div class="bfe--block-menu">
                                <button class="bfe--block-menu-button" @click=${(e: MouseEvent) => {
                                    this.dispatchEvent(new CustomEvent("use-layout", {
                                        detail: { index: this.index },
                                    }));
                                    e.preventDefault();
                                    e.stopPropagation();
                                    this._open_menu = false;
                                }}>
                                    Use Layout
                                </button>
                                ${!this.empty ? html`
                                    ${this._parentBlocks.map((block) => html`
                                        <div class="bfe--block-menu-group">
                                            <button class="bfe--block-menu-button" style="flex-grow: 1;" @click=${(e: MouseEvent) => {
                                                const uniquePath = block.uniquePath.split("/").slice(0, -1).join("/");
                                                this.dispatchEvent(new CustomEvent("add-block", {
                                                    detail: { 
                                                        index: this.index, 
                                                        uniquePath,
                                                        allowedblocks: block.allowedblocks ?? ""
                                                    },
                                                }));
                                                this._open_menu = false;
                                                e.stopPropagation();
                                                e.preventDefault();
                                            }}>
                                                ${window.blockFarmEditorDefinitions ? window.blockFarmEditorDefinitions[block.contentTypeKey ?? ''] : block.contentTypeKey} 
                                            </button>
                                            <button class="bfe--block-menu-button" title="Insert Layout" @click=${(e: MouseEvent) => {
                                                const uniquePath = block.uniquePath.split("/").slice(0, -1).join("/");
                                                this.dispatchEvent(new CustomEvent("use-layout", {
                                                    detail: {
                                                        index: this.index,
                                                        uniquePath
                                                    },
                                                }));
                                                e.stopPropagation();
                                                e.preventDefault();
                                                this._open_menu = false;
                                                }}>
                                                <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" class="bi bi-save" viewBox="0 0 16 16">
                                                    <path d="M2 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H9.5a1 1 0 0 0-1 1v7.293l2.646-2.647a.5.5 0 0 1 .708.708l-3.5 3.5a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L7.5 9.293V2a2 2 0 0 1 2-2H14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h2.5a.5.5 0 0 1 0 1z"/>
                                                </svg>
                                            </button>
                                        </div>
                                    `)}
                                `: ``}
                            </div>
                        ` : ''}
                    
                    ` : ''}
                </button>
            </div>
        `;
    }
}

declare global {
    interface HTMLElementTagNameMap {
        'add-block': AddBlock
    }
}