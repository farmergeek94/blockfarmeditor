import { css, html } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { type UmbModalContext, type UmbModalExtensionElement } from '@umbraco-cms/backoffice/modal';
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UrlHelper } from "../../../helpers/UrlHelper";
import { repeat } from "lit/directives/repeat.js";
import { UMB_AUTH_CONTEXT, UmbAuthContext } from "@umbraco-cms/backoffice/auth";
import type { UseLayoutModalData, UseLayoutModalResult } from "../tokens/use-layout-modal.token";

import type { Layout, Layouts } from '../../../models/Layout';
import type { BlockDefinition } from "../../../models/BlockDefinition";
import type { IBuilderProperties } from "../../../models/IBuilderProperties";
import type { PageDefinition } from "../../../models/PageDefinition";

import { GenerateGuid } from "../../../helpers/GuidHelper";


@customElement('bf-use-layout-modal')
export class UseLayoutModalElement extends UmbLitElement implements UmbModalExtensionElement<UseLayoutModalData, UseLayoutModalResult> {
    authContext?: UmbAuthContext;
    constructor() {
        super();
        this.consumeContext(UMB_AUTH_CONTEXT, (context) => {
            this.authContext = context;
        })
    }

    @state()
    private _filteredLayouts: Layouts = {};

    @state()
    private _searchTerm: string = '';
    
    
    @property({ attribute: false })
    modalContext?: UmbModalContext<UseLayoutModalData, UseLayoutModalResult>;

    @property({ attribute: false })
    data?: UseLayoutModalData;

    @state()
    private _layouts?: Layouts;

    firstUpdated() {
        this.authContext?.getLatestToken().then(token => 
            fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/getlayouts/`, {
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`
                },
                method: 'POST'
            }).then((response) => {
                if (response.ok) {
                    response.json().then((data: Layouts) => {
                        if (data) {    
                            for (const key in data) {
                                data[key] = data[key].filter(x => x.type == (this.data?.fullLayout ? 'pageDefinition' : 'blockArea'));
                            }

                            for (const key in data) {
                                if (data[key].length === 0) {
                                    delete data[key];
                                }
                            }

                            this._layouts = data;
                            this._filteredLayouts = this._layouts;
                        } else {
                            this._layouts = {};
                            this._filteredLayouts = {};
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
        
        if (this._layouts) {
            
            this._filteredLayouts = Object.entries(this._layouts).reduce((acc, [key, layouts]) => {
                const filteredLayouts = layouts.filter(layout =>
                    layout.name?.toLowerCase().includes(searchTerm) ||
                    layout.category?.toLowerCase().includes(searchTerm)
                );
                if (filteredLayouts.length > 0) {
                    acc[key] = filteredLayouts;
                }
                return acc;
            }, {} as Layouts);
        }
    }

    #rebuildUniques = (definition: BlockDefinition<IBuilderProperties>) => {
        if(definition.contentTypeKey) {
            definition.unique = GenerateGuid();
        }
        for (const block of definition.blocks) {
            this.#rebuildUniques(block);
        }
    }

    #handleLayoutClick = (layout: Layout) => {
        if(layout.type == 'blockArea') {
            const definition = JSON.parse(layout.layout) as BlockDefinition<IBuilderProperties>;
            // Rebuild uniques
            this.#rebuildUniques(definition);

            const result: UseLayoutModalResult = {
                block: definition,
                uniquePath: this.data?.uniquePath || '',
                index: this.data?.index
            };
            
            this.modalContext?.updateValue(result);
        } else if(layout.type == 'pageDefinition') {
            const definition = JSON.parse(layout.layout) as PageDefinition;
            const result: UseLayoutModalResult = {
                pageDefinition: definition,
                fullLayout: true,
                index: this.data?.index
            };
            this.modalContext?.updateValue(result);
        }
        this.modalContext?.submit();
    }

    #closeModal = () => {
        this.modalContext?.reject();
    }

    render() {
        if (!this._layouts) {
            return html`
                
                <umb-body-layout headline=${"Use Layout"}>
                    <div>
                        <p>Loading Layouts.</p>
                    </div>
                    <div slot="actions">
                        <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">
                            Cancel
                        </uui-button>
                    </div>
                </umb-body-layout>
            `;
        }

        if (this._layouts && (Object.keys(this._layouts).length == 0)) {
            return html`
                
                <umb-body-layout headline=${"Use Layout"}>
                    <div>
                        <p>No layouts available.</p>
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
                <umb-body-layout headline=${"Use Layout"}>
                
                <div class="modal-content">
                    <div class="search-container">
                        <uui-input 
                            type="text" 
                            placeholder="Search layouts..." 
                            @input="${this.#handleSearch}"
                            .value="${this._searchTerm}"
                        ></uui-input>
                    </div>

                    ${repeat(Object.entries(this._filteredLayouts), ([key, _]) => key, ([key, layouts]) => (
                        html`
                        <div class="section-container">
                            <h3 style="text-transform: capitalize;">${key}</h3>
                            <div class="items-grid">
                                ${layouts.map(layout => html`
                                    <div class="item-card" @click="${() => this.#handleLayoutClick(layout)}">
                                        ${layout.icon 
                                            ? html`<umb-icon .name="${layout.icon}"></umb-icon>`
                                            : html`<umb-icon name="icon-document"></umb-icon>`
                                        }
                                        <div class="item-details">
                                            <div class="item-name">${layout.name}</div>
                                            ${layout.description ? html`<div class="item-description">${layout.description}</div>` : ''}
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
        'bf-use-layout-modal': UseLayoutModalElement;
    }
}
