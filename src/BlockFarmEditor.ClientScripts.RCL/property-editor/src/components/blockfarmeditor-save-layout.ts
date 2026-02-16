import { css, html, type PropertyValues } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { type UmbModalContext, type UmbModalExtensionElement } from '@umbraco-cms/backoffice/modal';
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_AUTH_CONTEXT, UmbAuthContext } from "@umbraco-cms/backoffice/auth";

import type { Layout } from '../../../models/Layout';
import type { SaveLayoutModalData } from "../tokens/save-layout-modal.token";
import { UrlHelper } from '../../../helpers/UrlHelper';
import { UmbModalRouteRegistrationController } from "@umbraco-cms/backoffice/router";
import { UMB_ICON_PICKER_MODAL, type UmbIconPickerModalValue } from "@umbraco-cms/backoffice/icon";
import { UMB_NOTIFICATION_CONTEXT, UmbNotificationContext } from "@umbraco-cms/backoffice/notification";

@customElement('bf-save-layout-modal')
export class SaveLayoutModalElement extends UmbLitElement implements UmbModalExtensionElement<SaveLayoutModalData> {
    authContext?: UmbAuthContext;
    
    private notificationContext?: UmbNotificationContext;
    private _iconPickerModal: UmbModalRouteRegistrationController;
    
    constructor() {
        super();
        this.consumeContext(UMB_AUTH_CONTEXT, (context) => {
            this.authContext = context;
        })

        this._iconPickerModal = new UmbModalRouteRegistrationController(
                    this,
                    UMB_ICON_PICKER_MODAL
                )
                .onSubmit((result: UmbIconPickerModalValue) => {
                    if (result) {
                        this.layout = {...this.layout, icon: `${result.icon} ${result.color}`};
                    }
                });
        
            this.consumeContext(UMB_NOTIFICATION_CONTEXT, (instance) => {
                this.notificationContext = instance;
            });
    }

    @state()
    private layout: Layout = {
        id: -1,
        key: '',
        name: '',
        description: '',
        layout: '',
        category: '',
        type: 'blockArea',
        icon: '',
        enabled: true
    };
    
    @state()
    private categories: string[] = [];

    @state()
    private newCategory: boolean = false;

    @property({ attribute: false })
    modalContext?: UmbModalContext<SaveLayoutModalData>;

    @property({ attribute: false })
    data?: SaveLayoutModalData;

    protected firstUpdated(_changedProperties: PropertyValues): void {
        super.firstUpdated(_changedProperties);
        if (this.data) {
            this.layout = {...this.layout, layout: this.data.layout, type: this.data.fullLayout ? 'pageDefinition' : 'blockArea' };
        }
        this.fetchCategories();
    }   

    fetchCategories() {
        this.authContext?.getLatestToken().then((token) => {
          return fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/layouts/categories`, {
            headers: { Authorization: `Bearer ${token}` }
          }).then(response => response.json()).then(json => {
            if (json && json.data) {
              this.categories = json.data;
              this.newCategory = !this.categories.includes(this.layout.category);
            }
          })
        }).catch(error => {
          this.notificationContext?.peek("danger", {
            data: {
              message: error.message || 'An error occurred while fetching categories.',
              headline: 'Error fetching categories'
            }
          });
        })
      }

    #closeModal = () => {
        this.modalContext?.reject();
    }

    #saveModal = () => {
        this.authContext?.getLatestToken().then(token => {
                    this.layout = {...this.layout, layout: this.data?.layout || ''},
                    fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/layouts/create/`, {
                        credentials: 'include',
                        headers: {
                            'Content-Type': 'application/json',
                            Authorization: `Bearer ${token}`
                        },
                        body: JSON.stringify(this.layout),
                        method: 'POST'
                    }).then((response) => {
                        if (response.ok) {                  
                            this.modalContext?.submit();
                        } else {
                            console.error('Failed to fetch block definitions:', response.statusText);
                        }
                    }).catch((error) => {
                        console.error('Error fetching block definitions:', error);
                    });
                })
    }

    #selectIcon = () => {
        this._iconPickerModal.onSetup(() => ({
            data: { currentIcon: this.layout.icon },
            value: {} as any
        }));
        this._iconPickerModal.open({});
    }

    private _updateLayoutData(field: string, value: any) {
        if (field === 'category') {
            if (this.categories.includes(value)) {
                this.newCategory = false;
            } else {
                this.newCategory = true;
            }
        }
        this.layout = {
            ...this.layout,
            [field]: value
        }
    }

    render() {
        return html`
            <umb-body-layout headline=${"Save Layout"}>
           
                <div class="modal-content">
                    <uui-box>
                        <umb-property-layout label="Name" description="The name of the layout" ?mandatory=${true}>
                            <div slot="editor">
                                <uui-input
                                id="name"
                                type="text"
                                .value="${this.layout.name}"
                                maxlength="255"
                                @input="${(e: Event) => {
                                    const input = e.target as HTMLInputElement;
                                    this.layout = {...this.layout, name: input.value};
                                }}">
                                </uui-input>
                            </div>
                        </umb-property-layout>
                        <umb-property-layout label="Description" description="A brief description of the layout" ?mandatory=${true}>
                            <div slot="editor">
                                    
                                <uui-textarea
                                    id="description"
                                    type="text"
                                    .value="${this.layout.description}"
                                    maxlength="1000"
                                    @input="${(e: Event) => {
                                        const input = e.target as HTMLInputElement;
                                        this.layout = {...this.layout, description: input.value};
                                    }}">
                                </uui-textarea>
                                    
                            </div>
                        </umb-property-layout>
                        <umb-property-layout
                            name="Category"
                            description="The category of the layout."
                            orientation="horizontal"
                            label="Category"
                            ?mandatory=${true}
                            width="full"
                        >
                            <div slot="editor">
                            <uui-select
                                id="category"
                                @change=${(e: Event) => this._updateLayoutData('category', (e.target as HTMLSelectElement).value)}
                                placeholder="Select category"
                                .options=${[
                                    {
                                    name: 'New Category',
                                    value: '',
                                    selected: this.newCategory
                                    },
                                    ...this.categories.map(category => ({ name: category, value: category, selected: this.layout.category === category && !this.newCategory }))
                                ]}
                                required>
                            </uui-select>
                            ${this.newCategory ?
                                html`
                                    <uui-input
                                        class="new-category-input"
                                        id="category"
                                        .value=${this.layout.category}
                                        maxlength="255"
                                        @input=${(e: Event) => this._updateLayoutData('category', (e.target as HTMLInputElement).value)}
                                        placeholder="Enter category"
                                        >
                                    </uui-input>
                                ` : ''}
                            </div>
                        </umb-property-layout>
                        <umb-property-layout label="Icon" description="The icon of the layout">
                            <div slot="editor">
                                <div class="icon-preview">
                                    <uui-button look="secondary" @click=${this.#selectIcon}>Select Icon</uui-button>
                                    <umb-icon name="${this.layout.icon}"></umb-icon>
                                </div>
                            </div>
                        </umb-property-layout>
                        <umb-property-layout label="Enabled" description="Whether the layout is enabled">
                            <div slot="editor">
                                <uui-toggle
                                    id="enabled"
                                    .checked="${this.layout.enabled}"
                                    @change="${(e: Event) => {
                                        const input = e.target as HTMLInputElement;
                                        this.layout = {...this.layout, enabled: input.checked};
                                    }}">
                                </uui-toggle>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                </div>
                
                <div slot="actions">
                    <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">
                        Cancel
                    </uui-button>
                    <uui-button look="primary" label="Save" @click="${this.#saveModal}">
                        Save
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
        .modal-content uui-input {
            width: 100%;
        }
        .icon-preview {
            display: flex;
            align-items: center;
            gap: 10px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'bf-save-layout-modal': SaveLayoutModalElement;
    }
}
