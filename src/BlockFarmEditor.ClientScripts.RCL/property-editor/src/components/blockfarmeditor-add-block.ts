import { css, html } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { type UmbModalContext, type UmbModalExtensionElement } from '@umbraco-cms/backoffice/modal';
import type { AddBlockModalData, AddBlockModalResult } from '../tokens/add-block-modal.token';
import type BlockFarmEditorBlockDefinition from '../../../models/BlockFarmEditorBlockDefinition';
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UrlHelper } from "../../../helpers/UrlHelper";
import type { BlockDefinitions } from '../../../models/BlockDefinitions';
import { repeat } from "lit/directives/repeat.js";
import { UMB_AUTH_CONTEXT, UmbAuthContext } from "@umbraco-cms/backoffice/auth";

@customElement('bf-add-block-modal')
export class AddBlockModalElement extends UmbLitElement implements UmbModalExtensionElement<AddBlockModalData, AddBlockModalResult> {
    authContext?: UmbAuthContext;
    constructor() {
        super();
        this.consumeContext(UMB_AUTH_CONTEXT, (context) => {
            this.authContext = context;
        })
    }

    @state()
    private _filteredDefinitions: BlockDefinitions = {};

    @state()
    private _searchTerm: string = '';
    
    
    @property({ attribute: false })
    modalContext?: UmbModalContext<AddBlockModalData, AddBlockModalResult>;

    @property({ attribute: false })
    data?: AddBlockModalData;

    @state()
    private _blockDefinitions?: BlockDefinitions;

    firstUpdated() {
        this.authContext?.getLatestToken().then(token => 
            fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/getblockdefinitions/`, {
                credentials: 'include',
                body: JSON.stringify({allowedBlocks: this.data?.allowedBlocks}),
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`
                },
                method: 'POST'
            }).then((response) => {
                if (response.ok) {
                    response.json().then((data: BlockDefinitions) => {
                        this._blockDefinitions = data;
                        if (this._blockDefinitions) {
                            this._filteredDefinitions = this._blockDefinitions;
                        } else {
                            this._blockDefinitions = {};
                            this._filteredDefinitions = {};
                        }
                    });
                } else {
                    console.error('Failed to fetch block definitions:', response.statusText);
                }
            }).catch((error) => {
                console.error('Error fetching block definitions:', error);
            }));
    }

    #handleSearch = (e: InputEvent) => {
        const searchTerm = (e.target as HTMLInputElement).value.toLowerCase();
        this._searchTerm = searchTerm;
        
        if (this._blockDefinitions) {
            
            this._filteredDefinitions = Object.entries(this._blockDefinitions).reduce((acc, [key, blocks]) => {
                const filteredBlocks = blocks.filter(block =>
                    block.name?.toLowerCase().includes(searchTerm) ||
                    block.description?.toLowerCase().includes(searchTerm)
                );
                if (filteredBlocks.length > 0) {
                    acc[key] = filteredBlocks;
                }
                return acc;
            }, {} as BlockDefinitions);
        }
    }

    #handleBlockClick = (block: BlockFarmEditorBlockDefinition) => {
        const result: AddBlockModalResult = {
            block,
            uniquePath: this.data?.uniquePath || '',
            index: this.data?.index
        };
        
        this.modalContext?.updateValue(result);
        this.modalContext?.submit();
    }

    #closeModal = () => {
        this.modalContext?.reject();
    }

    render() {
        if (!this._blockDefinitions) {
            return html`
                
                <umb-body-layout headline=${"Add New Block"}>
                    <div>
                        <p>Loading Block Definitions.</p>
                    </div>
                    <div slot="actions">
                        <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">
                            Cancel
                        </uui-button>
                    </div>
                </umb-body-layout>
            `;
        }

        if (this._blockDefinitions && (Object.keys(this._blockDefinitions).length == 0)) {
            return html`
                
                <umb-body-layout headline=${"Add New Block"}>
                    <div>
                        <p>No block definitions available.</p>
                    </div>
                    <div slot="actions">
                        <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">
                            Cancel
                        </uui-button>
                    </div>
                </umb-body-layout>
            `;
        }

        return html`
                <umb-body-layout headline=${"Add New Block"}>
                
                <div class="modal-content">
                    <div class="search-container">
                        <uui-input 
                            type="text" 
                            placeholder="Search blocks and containers..." 
                            @input="${this.#handleSearch}"
                            .value="${this._searchTerm}"
                        ></uui-input>
                    </div>

                    ${repeat(Object.entries(this._filteredDefinitions), ([key, _]) => key, ([key, blocks]) => (
                        html`
                        <div class="section-container">
                            <h3 style="text-transform: capitalize;">${key}</h3>
                            <div class="items-grid">
                                ${blocks.map(block => html`
                                    <div class="item-card" @click="${() => this.#handleBlockClick(block)}">
                                        ${block.icon 
                                            ? html`<umb-icon .name="${block.icon}"></umb-icon>`
                                            : html`<umb-icon name="icon-document"></umb-icon>`
                                        }
                                        <div class="item-details">
                                            <div class="item-name">${block.name}</div>
                                            ${block.description ? html`<div class="item-description">${block.description}</div>` : ''}
                                        </div>
                                    </div>
                                `)}
                            </div>
                        </div>
                    `))}
                </div>
                
                <div slot="actions">
                    <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">
                        Cancel
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = css`
        .modal-content {
            overflow-y: auto;
            padding: var(--uui-size-space-4);
        }

        .search-container {
            margin-bottom: var(--uui-size-space-5);
        }

        .search-container uui-input {
            width: 100%;
        }

        .section-container {
            margin-bottom: var(--uui-size-space-5);
        }

        .section-container:last-child {
            margin-bottom: 0;
        }

        h3 {
            margin: 0 0 var(--uui-size-space-3) 0;
            padding: 0 0 var(--uui-size-space-2) 0;
            font-size: var(--uui-type-h5-size);
            font-weight: 500;
            border-bottom: 1px solid var(--uui-color-border);
            color: var(--uui-color-text);
        }

        .items-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            gap: var(--uui-size-space-3);
            margin-top: var(--uui-size-space-3);
        }

        .item-card {
            display: flex;
            align-items: center;
            padding: var(--uui-size-space-4);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background-color: var(--uui-color-surface);
            cursor: pointer;
            transition: all 0.2s ease;
        }

        .item-card:hover {
            background-color: var(--uui-color-surface-emphasis);
            border-color: var(--uui-color-border-emphasis);
            transform: translateY(-1px);
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        .item-card:active {
            transform: translateY(0);
        }

        .item-card umb-icon {
            margin-right: var(--uui-size-space-4);
            font-size: 24px;
            flex-shrink: 0;
        }

        .item-details {
            flex: 1;
            min-width: 0; /* Allow text truncation */
        }

        .item-name {
            font-weight: 600;
            color: var(--uui-color-text);
            margin-bottom: var(--uui-size-space-1);
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .item-description {
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .empty-list {
            color: var(--uui-color-text-alt);
            font-style: italic;
            padding: var(--uui-size-space-4);
            text-align: center;
            background-color: var(--uui-color-surface-alt);
            border-radius: var(--uui-border-radius);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'bf-add-block-modal': AddBlockModalElement;
    }
}
