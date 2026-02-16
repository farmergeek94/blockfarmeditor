import { html } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { BlockElement } from "./BlockElement";
import { unsafeHTML } from "lit/directives/unsafe-html.js";

/**
 * An example element.
 *
 * @slot - This element has a slot
 * @csspart button - The button
 */
@customElement('block-item')
export class Block extends BlockElement {
    constructor() {
        super()
        this.blockUpdate = this.blockUpdate.bind(this)
        this.hoverMove = this.hoverMove.bind(this)
        this.hoverLeave = this.hoverLeave.bind(this)
    }

    @property({ type: String })
    public contentTypeKey?: string;

    @property({ type: Number })
    public index?: number;

    @property({ type: Boolean })
    public lastblock: boolean = false;

    @property({ type: Boolean })
    public hasproperties?: boolean;

    @property({ type: String })
    public html?: string;

    @property({ type: String })
    public parentUniquePath: string = "";

    @state()
    public parentBlocks: Block[] = [];

    @state()
    public showParentProperties: boolean = false;

    connectedCallback(): void {
        super.connectedCallback()
        window.addEventListener('block-update', this.blockUpdate)
        this.addEventListener('mouseover', this.hoverMove)
        this.addEventListener('mouseleave', this.hoverLeave)
        this.setParentBlocksWithProperties();
    }

    disconnectedCallback(): void {
        super.disconnectedCallback()
        window.removeEventListener('block-update', this.blockUpdate)
        this.removeEventListener('mouseover', this.hoverMove)
        this.removeEventListener('mouseleave', this.hoverLeave)
    }

    private hoverMove() {
        document.body.querySelectorAll('block-properties').forEach((el) => {
            (el as HTMLElement).classList.remove('bfe--show');
        });
        var el = this.querySelector(':scope > block-properties');
        el?.classList.add('bfe--show');
    }

    private hoverLeave() {
        document.body.querySelectorAll('block-properties').forEach((el) => {
            (el as HTMLElement).classList.remove('bfe--show');
        });
        this.showParentProperties = false;
    }

    private blockUpdate() {
        this.dispatchEvent(new CustomEvent('block-update', {
            detail: this.uniquePath
        }));
    }

    private setParentBlocksWithProperties() {
        const parentBlocks: Block[] = [];
        let currentElement = this.parentElement;
        
        while (currentElement) {
            const parentBlock = currentElement.closest('block-item') as Block;
            if (!parentBlock) break;
            
            parentBlocks.push(parentBlock);
            
            // Move up the DOM tree
            currentElement = parentBlock.parentElement;
        }
        
        this.parentBlocks = parentBlocks;
    }

    
    private addBlock(detail?: any) {
        window.parent.postMessage({
            messageType: 'blockfarmeditor:add-block',
            uniquePath: detail.uniquePath ?? this.parentUniquePath,
            index: detail.index,
            allowedblocks: detail.allowedblocks ?? this.allowedblocks
        }, window.location.origin)
    }
    
    private useLayout(detail?: any) {
        window.parent.postMessage({
            messageType: 'blockfarmeditor:use-layout',
            uniquePath: detail.uniquePath ?? this.parentUniquePath,
            index: detail.index
        }, window.location.origin)
    }

    private handleBlockMoveStart(e: CustomEvent) {
        e.stopPropagation();
    }

    render() {
        return html`
            <!-- Add a hover area at the top of the block for adding new blocks   -->
            <add-block
                class="bfe--hover-top"
                .index="${this.index ?? 0}"  
                @add-block=${(e: CustomEvent) => this.addBlock(e.detail)}
                @use-layout=${(e: CustomEvent) => { this.useLayout(e.detail) }}>
            </add-block>
            ${unsafeHTML(this.html ?? '')}
            <!-- Add a hover area at the bottom of the block for adding new blocks -->
            <add-block
                class="bfe--hover-bottom"
                .index="${(this.index ?? 0) + 1}"
                ?lastblock="${this.lastblock}"
                @add-block=${(e: CustomEvent) => this.addBlock(e.detail)}
                @use-layout=${(e: CustomEvent) => { this.useLayout(e.detail) }}>
            </add-block>  
            <block-properties 
                ?showParentProperties=${this.showParentProperties}
                .parentBlocks=${this.parentBlocks as any}
                ?hasproperties=${this.hasproperties}
                .contentTypeKey=${this.contentTypeKey}
                .uniquePath=${this.uniquePath}
                .index=${this.index}
                @block-move-start=${this.handleBlockMoveStart}
                @parent-properties-toggle=${(e: CustomEvent) => {
                    e.stopPropagation();
                    this.showParentProperties = e.detail;
                }}
                >
            </block-properties>
        `
    }
}

declare global {
    interface HTMLElementTagNameMap {
        'block-item': Block
    }
}