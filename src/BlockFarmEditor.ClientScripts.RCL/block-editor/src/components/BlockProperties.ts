import { LitElement, css, html } from "lit";
import { customElement, property } from 'lit/decorators.js'
import type { Block } from "./Block";

@customElement('block-properties')
export class BlockProperties extends LitElement {
    @property({ type: String })
    public contentTypeKey?: string;

    @property({ type: String })
    public uniquePath = ''

    @property({ type: Boolean })
    public hasproperties?: boolean;

    @property({ type: Array })
    public parentBlocks: Block[] = [];

    @property({ type: Boolean })
    public showParentProperties: boolean = false;

    @property({ type: Number })
    public index?: number;

    render() {
        return html`
            <div class="bfe--block-area-block-buttons-wrapper">
                <div>
                    ${window.blockFarmEditorDefinitions ? window.blockFarmEditorDefinitions[this.contentTypeKey ?? ''] : this.contentTypeKey}
                </div>
                <block-actions
                    .uniquePath=${this.uniquePath}
                    ?hasProperties=${this.hasproperties}
                    ?showParentProperties=${this.showParentProperties}
                    ?hasParents=${this.parentBlocks.length > 0}
                    .index=${this.index}
                    >
                </block-actions>
                
            </div>
            ${this.showParentProperties ? html`
                <div class="bfe--block-parent-properties">
                    ${this.parentBlocks.map((parentBlock) => html`
                        <div class="bfe--block-area-block-buttons-wrapper">
                            <div>
                                ${window.blockFarmEditorDefinitions ? window.blockFarmEditorDefinitions[parentBlock.contentTypeKey ?? ''] : parentBlock.contentTypeKey}
                            </div>
                            <block-actions
                                .uniquePath=${parentBlock.uniquePath}
                                ?hasProperties=${parentBlock.hasproperties}
                                .index=${parentBlock.index}
                                >
                            </block-actions>
                        </div>
                    `)}
                </div>
            ` : ''}
        `;
    }

    static styles = css`
    :host {
        color: #333; /* or your desired color */
    }
    .bfe--block-parent-properties {
        width: 100%;
    }

    .bfe--block-area-block-buttons-wrapper {
        display: flex;
        flex-direction: row;
        align-items: center;
        justify-content: flex-end;
        border-bottom: 1px dotted #ccc;
        width: 100%;
    }

    .bfe--block-area-block-buttons-wrapper:last-child {
        border-bottom: none;
    }
    `
}

declare global {
    interface HTMLElementTagNameMap {
        'block-properties': BlockProperties
    }
}