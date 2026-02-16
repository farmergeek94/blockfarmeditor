import { LitElement, css, html } from "lit";
import { customElement, property } from 'lit/decorators.js'
import { sendPropertiesMessage } from "../helpers/Actions";

@customElement('block-actions')
export class BlockActions extends LitElement {
    @property({ type: String })
    public uniquePath: string = '';

    @property({ type: Boolean })
    public hasProperties: boolean = false;

    @property({ type: Boolean })
    public hasParents: boolean = false;

    @property({ type: Boolean })
    public showParentProperties: boolean = false;

    @property({ type: Number })
    public index?: number;

    private blockDelete() {
        const parentPath = this.uniquePath.split('/').slice(0, -1).join('/');
        window.dispatchEvent(new CustomEvent('block-farm-editor', {
            detail: {
                type: "block-delete",
                uniquePath: this.uniquePath,
                parentPath
            },
            bubbles: true,
            composed: true
        }))
    }

    private blockSaveLayout() {
        const parentPath = this.uniquePath.split('/').slice(0, -1).join('/');
        window.dispatchEvent(new CustomEvent('block-farm-editor', {
            detail: {
                type: "block-save-layout",
                uniquePath: this.uniquePath,
                parentPath,
            },
            bubbles: true,
            composed: true
        }))
    }

    private blockProperties() {
        const block = window.blockFarmEditor.getBlock(this.uniquePath)
        if (block) {
            sendPropertiesMessage('edit', block, this.uniquePath)
        }
    }

    private blockMoveStart() {
        const parentPath = this.uniquePath.split('/').slice(0, -1).join('/');
        window.dispatchEvent(new CustomEvent('block-farm-editor', {
            detail: {
                type: "block-move-start",
                uniquePath: this.uniquePath,
                parentPath,
                index: this.index
            },
            bubbles: true,
            composed: true
        }))
        this.dispatchEvent(new Event("block-move-start", {
            bubbles: true,
            composed: true
        }))
    }
    
    private toggleParentProperties() {
        this.dispatchEvent(new CustomEvent('parent-properties-toggle', {
            detail: !this.showParentProperties
            , bubbles: true
            , composed: true
        }));
    }

    render() {
        return html`
            <button class="block-button-delete" @click=${() => { this.blockDelete() }} title="Delete Block">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-trash" viewBox="0 0 16 16">
                    <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0z"/>
                    <path d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4zM2.5 3h11V2h-11z"/>
                </svg>
            </button>
            ${this.hasProperties ? html`<div class="block-button-group">
                <button class="block-button-properties" @click=${() => { this.blockProperties() }} title="Edit Block Properties">
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-gear" viewBox="0 0 16 16">
                        <path d="M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492M5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0"/>
                        <path d="M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.377l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115z"/>
                    </svg>
                </button>
            </div>` : ''}
            
            ${this.hasParents ? html`<button class="block-button-parent-properties" @click=${() => { this.toggleParentProperties() }} title="Toggle Parent Properties">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-caret-down-fill" viewBox="0 0 16 16">
                    <path d="M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"/>
                </svg>
            </button>` : ''}
            <button class="block-button-save-layout" @click=${() => { this.blockSaveLayout() }} title="Save as Layout">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-floppy" viewBox="0 0 16 16">
                    <path d="M11 2H9v3h2z"/>
                    <path d="M1.5 0h11.586a1.5 1.5 0 0 1 1.06.44l1.415 1.414A1.5 1.5 0 0 1 16 2.914V14.5a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 0 14.5v-13A1.5 1.5 0 0 1 1.5 0M1 1.5v13a.5.5 0 0 0 .5.5H2v-4.5A1.5 1.5 0 0 1 3.5 9h9a1.5 1.5 0 0 1 1.5 1.5V15h.5a.5.5 0 0 0 .5-.5V2.914a.5.5 0 0 0-.146-.353l-1.415-1.415A.5.5 0 0 0 13.086 1H13v4.5A1.5 1.5 0 0 1 11.5 7h-7A1.5 1.5 0 0 1 3 5.5V1H1.5a.5.5 0 0 0-.5.5m3 4a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5V1H4zM3 15h10v-4.5a.5.5 0 0 0-.5-.5h-9a.5.5 0 0 0-.5.5z"/>
                </svg>
            </button>
            <button class="block-button-move" @mousedown=${() => { this.blockMoveStart() }} title="Move Block">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-arrows-move" viewBox="0 0 16 16">
                    <path fill-rule="evenodd" d="M7.646.146a.5.5 0 0 1 .708 0l2 2a.5.5 0 0 1-.708.708L8.5 1.707V5.5a.5.5 0 0 1-1 0V1.707L6.354 2.854a.5.5 0 1 1-.708-.708zM8 10a.5.5 0 0 1 .5.5v3.793l1.146-1.147a.5.5 0 0 1 .708.708l-2 2a.5.5 0 0 1-.708 0l-2-2a.5.5 0 0 1 .708-.708L7.5 14.293V10.5A.5.5 0 0 1 8 10M.146 8.354a.5.5 0 0 1 0-.708l2-2a.5.5 0 1 1 .708.708L1.707 7.5H5.5a.5.5 0 0 1 0 1H1.707l1.147 1.146a.5.5 0 0 1-.708.708zM10 8a.5.5 0 0 1 .5-.5h3.793l-1.147-1.146a.5.5 0 0 1 .708-.708l2 2a.5.5 0 0 1 0 .708l-2 2a.5.5 0 0 1-.708-.708L14.293 8.5H10.5A.5.5 0 0 1 10 8"/>
                </svg>
            </button>
        `;
    }
    static styles = css`
        :host {
            display: flex;
            flex-direction: row;
            align-items: center;
            justify-content: flex-end;
        }
        button {
            background: white;
            color: #333;
            border: none;
            cursor: pointer;
            font-size: 8px !important;
            line-height: 8px !important;
            padding: 4px !important;
            border-radius: 3px;
        }

        button:hover {
            background: rgb(27, 38, 79);
            color: white;
        }

        button.block-button-delete:hover {
            background: rgb(255, 0, 0);
            color: white;
        }

        button:focus {
            outline: none;
        }
    `
}

declare global {
    interface HTMLElementTagNameMap {
        'block-actions': BlockActions
    }
}