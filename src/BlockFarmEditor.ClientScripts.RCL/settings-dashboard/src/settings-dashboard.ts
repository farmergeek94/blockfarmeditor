import { css, html, type PropertyValues } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { UMB_AUTH_CONTEXT, UmbAuthContext } from '@umbraco-cms/backoffice/auth'
import type { Layout, Layouts } from '../../models/Layout'
import { UMB_NOTIFICATION_CONTEXT, UmbNotificationContext } from '@umbraco-cms/backoffice/notification'
import { repeat } from 'lit/directives/repeat.js'
import { UrlHelper } from '../../helpers/UrlHelper';

/**
 * An example element.
 *
 * @slot - This element has a slot
 * @csspart button - The button
 */
@customElement('blockfarmeditor-settings-dashboard')
export class SettingsDashboard extends UmbLitElement implements UmbPropertyEditorUiElement {
    authContext?: UmbAuthContext
    notificationContext?: UmbNotificationContext
    constructor() {
        super()
        this.consumeContext(UMB_AUTH_CONTEXT, (context) => {
            this.authContext = context;
        });
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.notificationContext = context;
        });
    }

    connectedCallback(): void {
        super.connectedCallback();
    }

    @state()
    private layouts: Layouts = {};

    @state()
    private _filteredLayouts: Layouts = {};

    @state()
    private _searchTerm: string = '';

    protected firstUpdated(_changedProperties: PropertyValues): void {
        super.firstUpdated(_changedProperties);
        this.loadLayouts();
    }

    #handleSearch = (e: InputEvent) => {
        const searchTerm = (e.target as HTMLInputElement).value.toLowerCase();
        this._searchTerm = searchTerm;

        if (this.layouts) {

            this._filteredLayouts = Object.entries(this.layouts).reduce((acc, [key, layouts]) => {
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

    private loadLayouts() {
        this.authContext?.getLatestToken().then(x => fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/getlayouts`, {
            credentials: 'include',
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${x}`
            }
        }).then((response) => {
            if (response.ok) {
                response.json().then((data) => {
                    this.layouts = data ?? {};
                    this._filteredLayouts = { ...this.layouts };
                });
            } else {
                this.notificationContext?.peek('danger', {
                    data: {
                        message: 'Failed to retrieve layouts: ' + response.statusText,
                    },
                    action: {
                        label: 'Retry',
                        onClick: () => this.loadLayouts()
                    }
                });
            }
        }));
    }

    #exportDefinitions() {
        this.authContext?.getLatestToken().then(x => fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions/export`, {
            credentials: 'include',
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${x}`
            }
        }).then((response) => {
            if (response.ok) {
                this.notificationContext?.peek('positive', {
                    data: {
                        message: 'Definitions exported successfully',
                    }
                });
            } else {
                this.notificationContext?.peek('danger', {
                    data: {
                        message: 'Failed to export definitions: ' + response.statusText,
                    },
                    action: {
                        label: 'Retry',
                        onClick: () => this.#exportDefinitions()
                    }
                });
            }
        }));
    }

    #importDefinitions() {
        this.authContext?.getLatestToken().then(x => fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions/import`, {
            credentials: 'include',
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${x}`
            }
        }).then((response) => {
            if (response.ok) {
                this.notificationContext?.peek('positive', {
                    data: {
                        message: 'Definitions imported successfully',
                    }
                });
            } else {
                this.notificationContext?.peek('danger', {
                    data: {
                        message: 'Failed to import definitions: ' + response.statusText,
                    },
                    action: {
                        label: 'Retry',
                        onClick: () => this.#importDefinitions()
                    }
                });
            }
        }));
    }

    #deleteLayout(layout: Layout) {
        this.authContext?.getLatestToken().then(token => {
            fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/layouts/delete/${layout.key}`, {
                credentials: 'include',
                headers: {
                    'Authorization': `Bearer ${token}`
                },
                method: 'DELETE'
            }).then((response) => {
                if (response.ok) {
                    this.notificationContext?.peek('positive', {
                        data: {
                            message: 'Layout deleted successfully',
                        }
                    });
                    this.loadLayouts();
                } else {
                    this.notificationContext?.peek('danger', {
                        data: {
                            message: 'Failed to delete layout: ' + response.statusText,
                        },
                        action: {
                            label: 'Retry',
                            onClick: () => this.#deleteLayout(layout)
                        }
                    });
                }
            });
        });
    }

    render() {
        return html`
    <uui-box class="text-left" headline="Layouts Management">
        <p style="margin-top: 0;">Layouts are reusable block structures that can be saved and applied to blocks.</p>
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
                        <div class="item-card">
                            ${layout.icon 
                                ? html`<umb-icon .name="${layout.icon}"></umb-icon>`
                                : html`<umb-icon name="icon-document"></umb-icon>`
                            }
                            <div class="item-details">
                                <div class="item-name">${layout.name}</div>
                                ${layout.description ? html`<div class="item-description">${layout.description}</div>` : ''}
                            </div>
                            <div class="item-actions">
                                <uui-button look="secondary" @click="${() => this.#deleteLayout(layout)}"><umb-icon name="icon-trash" color="red"></umb-icon></uui-button>
                            </div>
                        </div>
                    `)}
                </div>
            </div>
        `))}
    </uui-box>
    `
    }

    static styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }

    .text-left {
      text-align: left;
    }
    
    .button-group {
      display: flex;
      gap: var(--uui-size-layout-1);
    }

    uui-label {
      display: block;
    }

    uui-select {
      min-width: 200px;
    }

    uui-textarea, uui-input, uui-select {
      margin-bottom: var(--uui-size-layout-1);
    }

    .messages {
      margin-top: var(--uui-size-layout-1);
    }
    uui-box {
      margin-bottom: var(--uui-size-layout-1);
    }

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

    .item-card:active {
        transform: translateY(0);
    }

    .item-card umb-icon {
        margin-right: var(--uui-size-space-4);
        font-size: 24px;
        flex-shrink: 0;
    }

    .item-actions umb-icon {
        margin-right: 0;
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
  `
}

declare global {
    interface HTMLElementTagNameMap {
        'blockfarmeditor-settings-dashboard': SettingsDashboard
    }
}
