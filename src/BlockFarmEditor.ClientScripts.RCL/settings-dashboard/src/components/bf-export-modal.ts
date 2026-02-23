import { css, html } from "lit";
import { customElement, state } from "lit/decorators.js";
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import type { ExportModalData, ExportModalResult, ExportDefinitionItem } from '../tokens/export-modal.token';
import { UMB_AUTH_CONTEXT, type UmbAuthContext } from "@umbraco-cms/backoffice/auth";
import { UrlHelper } from "../../../helpers/UrlHelper";
import { repeat } from "lit/directives/repeat.js";

interface GroupedDefinitions {
    [category: string]: ExportDefinitionItem[];
}

@customElement('bf-export-modal')
export class ExportModalElement extends UmbModalBaseElement<ExportModalData, ExportModalResult> {
    private authContext?: UmbAuthContext;

    constructor() {
        super();
        this.consumeContext(UMB_AUTH_CONTEXT, (context) => {
            this.authContext = context;
        });
    }

    @state()
    private _definitions: ExportDefinitionItem[] = [];

    @state()
    private _filteredDefinitions: GroupedDefinitions = {};

    @state()
    private _searchTerm: string = '';

    @state()
    private _download: boolean = false;

    @state()
    private _loading: boolean = true;

    firstUpdated() {
        this._loadDefinitions();
    }

    private async _loadDefinitions() {
        this._loading = true;
        try {
            const token = await this.authContext?.getLatestToken();
            const response = await fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions/exportable`, {
                credentials: 'include',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                const data = await response.json() as ExportDefinitionItem[];
                this._definitions = data.map(d => ({ ...d, selected: false }));
                this._applyFilter();
            }
        } catch (error) {
            console.error('Failed to load definitions:', error);
        } finally {
            this._loading = false;
        }
    }

    #handleSearch = (e: InputEvent) => {
        this._searchTerm = (e.target as HTMLInputElement).value.toLowerCase();
        this._applyFilter();
    }

    private _applyFilter() {
        const filtered = this._definitions.filter(def =>
            def.alias?.toLowerCase().includes(this._searchTerm) ||
            def.category?.toLowerCase().includes(this._searchTerm) ||
            def.name?.toLowerCase().includes(this._searchTerm)
        );

        this._filteredDefinitions = filtered.reduce((acc, def) => {
            const category = def.category || 'Uncategorized';
            if (!acc[category]) {
                acc[category] = [];
            }
            acc[category].push(def);
            return acc;
        }, {} as GroupedDefinitions);
    }

    #handleCheckboxChange = (def: ExportDefinitionItem) => {
        def.selected = !def.selected;
        this._definitions = [...this._definitions];
        this._applyFilter();
    }

    #handleSelectAllToggle = (e: Event) => {
        const checked = (e.target as HTMLInputElement).checked;
        this._definitions.forEach(d => d.selected = checked);
        this._definitions = [...this._definitions];
        this._applyFilter();
    }

    get _allSelected(): boolean {
        return this._definitions.length > 0 && this._definitions.every(d => d.selected);
    }

    get _someSelected(): boolean {
        return this._definitions.some(d => d.selected) && !this._allSelected;
    }

    #handleDownloadToggle = (e: Event) => {
        this._download = (e.target as HTMLInputElement).checked;
    }

    #handleSubmit = () => {
        const selectedKeys = this._definitions
            .filter(d => d.selected)
            .map(d => d.key);

        const result: ExportModalResult = {
            selectedDefinitions: selectedKeys,
            download: this._download
        };

        this.modalContext?.updateValue(result);
        this.modalContext?.submit();
    }

    #closeModal = () => {
        this.modalContext?.reject();
    }

    get _selectedCount(): number {
        return this._definitions.filter(d => d.selected).length;
    }

    render() {
        if (this._loading) {
            return html`
                <umb-body-layout headline="Export Definitions">
                    <div class="loading">
                        <uui-loader></uui-loader>
                        <p>Loading definitions...</p>
                    </div>
                    <div slot="actions">
                        <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">Cancel</uui-button>
                    </div>
                </umb-body-layout>
            `;
        }

        return html`
            <umb-body-layout headline="Export Definitions" scrollable>
                <div class="modal-content">
                    <p class="description">
                        Select the definitions to export. The export will include linked element types, 
                        data types, and partial views.
                    </p>

                    <div class="search-container">
                        <uui-input 
                            type="text" 
                            placeholder="Search definitions..." 
                            @input="${this.#handleSearch}"
                            .value="${this._searchTerm}"
                        ></uui-input>
                    </div>

                    <div class="selection-actions">
                        <uui-checkbox
                            .checked="${this._allSelected}"
                            ?indeterminate="${this._someSelected}"
                            @change="${this.#handleSelectAllToggle}"
                            label="Select all"
                        ></uui-checkbox>
                        <span class="selection-count">${this._selectedCount} of ${this._definitions.length} selected</span>
                    </div>

                    <div class="definitions-list">
                        ${repeat(Object.entries(this._filteredDefinitions), ([key]) => key, ([category, defs]) => html`
                            <div class="section-container">
                                <h3>${category}</h3>
                                <div class="items-grid">
                                    ${defs.map(def => html`
                                        <div class="item-card" 
                                             @click="${() => this.#handleCheckboxChange(def)}">
                                            <uui-checkbox 
                                                .checked="${def.selected || false}"
                                                @click="${(e: Event) => e.stopPropagation()}"
                                                @change="${() => this.#handleCheckboxChange(def)}"
                                            ></uui-checkbox>
                                            ${def.icon 
                                                ? html`<umb-icon .name="${def.icon}"></umb-icon>`
                                                : html`<umb-icon name="icon-document"></umb-icon>`
                                            }
                                            <div class="item-details">
                                                <div class="item-name">${def.name}</div>
                                                ${def.description && def.description.trim().length > 0 ? html`<div class="item-description">${def.description}</div>` : ''}
                                            </div>
                                        </div>
                                    `)}
                                </div>
                            </div>
                        `)}
                    </div>

                    <div class="options-section">
                        <h3>Export Options</h3>
                        <div class="option-row">
                            <uui-toggle 
                                .checked="${this._download}"
                                @change="${this.#handleDownloadToggle}"
                                label="Download ZIP file"
                            ></uui-toggle>
                            <span class="option-description">
                                ${this._download 
                                    ? 'Export will download as ZIP and save to server folder' 
                                    : 'Export will save to BlockFarmEditor folder only'}
                            </span>
                        </div>
                    </div>
                </div>

                <div slot="actions">
                    <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">Cancel</uui-button>
                    <uui-button 
                        look="primary" 
                        label="Export" 
                        @click="${this.#handleSubmit}"
                        ?disabled="${this._selectedCount === 0}"
                    >
                        Export (${this._selectedCount})
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = css`
        .modal-content {
            padding: var(--uui-size-space-4);
            overflow-y: auto;
            display: flex;
            flex-direction: column;         
            max-height: 100%;
            box-sizing: border-box;
        }

        .description {
            margin: 0 0 var(--uui-size-space-4) 0;
            color: var(--uui-color-text-alt);
            flex: 0;
        }

        .loading {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--uui-size-space-6);
        }

        .search-container {
            margin-bottom: var(--uui-size-space-4);
            flex: 0;
        }

        .search-container uui-input {
            width: 100%;
        }

        .selection-actions {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
            padding-bottom: var(--uui-size-space-3);
            border-bottom: 1px solid var(--uui-color-border);
            flex: 0;
        }

        .selection-count {
            margin-left: auto;
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .definitions-list {
            overflow-y: auto;
            margin-bottom: var(--uui-size-space-4);
            flex: 1;
        }

        .section-container {
            margin-bottom: var(--uui-size-space-4);
        }

        h3 {
            margin: 0 0 var(--uui-size-space-3) 0;
            padding: 0 0 var(--uui-size-space-2) 0;
            font-size: var(--uui-type-h5-size);
            font-weight: 500;
            border-bottom: 1px solid var(--uui-color-border);
            color: var(--uui-color-text);
            text-transform: capitalize;
        }

        .items-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
            gap: var(--uui-size-space-3);
        }

        .item-card {
            display: flex;
            align-items: center;
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background-color: var(--uui-color-surface);
            cursor: pointer;
            transition: all 0.2s ease;
            gap: var(--uui-size-space-3);
        }

        .item-card:hover {
            background-color: var(--uui-color-surface-emphasis);
            border-color: var(--uui-color-border-emphasis);
        }

        .item-card umb-icon {
            margin-right: var(--uui-size-space-4);
            font-size: 24px;
            flex-shrink: 0;
        }

        .item-details {
            flex: 1;
            min-width: 0;
        }

        .item-name {
            font-weight: 600;
            color: var(--uui-color-text);
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

        .options-section {
            padding-top: var(--uui-size-space-4);
            border-top: 1px solid var(--uui-color-border);
            flex: 0;
        }

        .option-row {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
        }

        .option-description {
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'bf-export-modal': ExportModalElement;
    }
}
