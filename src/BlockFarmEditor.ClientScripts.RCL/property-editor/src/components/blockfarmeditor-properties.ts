import type { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';
import type { UmbPropertyValueData } from '@umbraco-cms/backoffice/property';
import type { ManifestPropertyEditorSchema, UmbPropertyEditorConfig } from '@umbraco-cms/backoffice/property-editor';
import { css, html, nothing } from "lit";
import { customElement, state } from "lit/decorators.js";
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import type { BlockPropertiesModalData, BlockPropertiesModalResult } from '../tokens/block-properties-modal.token';
import { UrlHelper } from '../../../helpers/UrlHelper';
import type { BlockDefinitions } from '../../../models/BlockDefinitions';
import type BlockFarmEditorBlockDefinition from '../../../models/BlockFarmEditorBlockDefinition';
import { UMB_AUTH_CONTEXT, UmbAuthContext } from '@umbraco-cms/backoffice/auth';
import { repeat } from 'lit/directives/repeat.js';
import type { UmbPropertyTypeValidationModel } from '@umbraco-cms/backoffice/content-type';
import { UmbValidationContext } from '@umbraco-cms/backoffice/validation';
import { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';

interface BlockProperty {
    alias: string;
    label: string;
    description?: string;
    configurations?: UmbPropertyEditorConfig;
    validation?: UmbPropertyTypeValidationModel;
}

interface BlockProperties {
    [key: string]: BlockProperty;
}

interface BlockGroup {
    alias: string
    label?: string
    type: number // 1 = Tab, 0 = Group
    editors?: BlockProperties
    groups?: BlockGroup[]
}

@customElement('bf-block-properties-modal')
export default class BlockPropertiesModalElement extends UmbModalBaseElement<BlockPropertiesModalData, BlockPropertiesModalResult> {
    authContext?: UmbAuthContext;
    #validation: UmbValidationContext;
    #notificationContext?: UmbNotificationContext;
    constructor(){
        super();
        this.consumeContext(UMB_AUTH_CONTEXT, context => {
            this.authContext = context;
        })
        this.#validation = new UmbValidationContext(this);

        this.#notificationContext = new UmbNotificationContext(this);
    }

    @state()
    private _tabs: BlockGroup[] = [];

    @state() 
    private _selectedGroup?: BlockGroup

    @state()
    private _properties: UmbPropertyValueData<any>[] = [];

    @state()
    private _loading: boolean = true;

    @state()
    private _blockDefinitions?: BlockFarmEditorBlockDefinition[];

    async firstUpdated() {
        if (!this.data?.block.contentTypeKey) {
            this._loading = false;
            return;
        }
        console.log(this.data)
            // Parse properties from the message
        if (this.data.block.properties) {
            if (Array.isArray(this.data.block.properties)) {
            this._properties = this.data.block.properties;
            } else if (typeof this.data.block.properties === 'object') {
            this._properties = Object.keys(this.data.block.properties).map((key) => ({
                alias: key,
                value: this.data?.block.properties?.[key]
            }));
            }
        }

        // Set initial properties
        try {
            const authToken = await this.authContext?.getLatestToken();

            const response = await fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/getpropertyeditors?contentTypeKey=${this.data.block.contentTypeKey}`, {
                credentials: 'include',
                headers:{
                    Authorization: `Bearer ${authToken}`
                }
            });
            
            if (response.ok) {
                const data: BlockGroup[] = await response.json();
                if (data && data.length > 0) {
                    const groups = data.filter(x=>x.type != 1);
                    if(groups.length > 0) {
                        this._selectedGroup = {
                            alias: "",
                            type: 0,
                            groups
                        }
                    } else {
                        this._tabs = data;
                        if( this._tabs.length > 0) {
                            this._selectedGroup = this._tabs[0];
                        }
                    }
                }
            } else {
                console.error('Failed to fetch block properties:', response.statusText);
                this._rejectModal();
            }
        } catch (error) {
            console.error('Error fetching block properties:', error);
            this._rejectModal();
        } finally {
            this._loading = false;
        }

        fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/getblockdefinitions/`, {
                    credentials: 'include',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    method: 'POST',
        }).then((response) => {
            if (response.ok) {
                response.json().then((data: BlockDefinitions) => {
                    this._blockDefinitions = Object.entries(data).reduce((acc, [_, value]) => {
                return [...acc, ...value];
            }, [] as BlockFarmEditorBlockDefinition[]);
                });
            } else {
                console.error('Failed to fetch block definitions:', response.statusText);
            }
        }).catch((error) => {
            console.error('Error fetching block definitions:', error);
        });
    }   
    
    #onDataChange = (event: UmbChangeEvent) => {
        this._properties = (event.target as any).value;
    };

    #getBlockTypeName(): string {
        if (this._blockDefinitions) {
            const definition = this._blockDefinitions .find((def) => def.contentTypeKey === this.data?.block.contentTypeKey);
            return definition?.name || this.data?.block.contentTypeKey || 'Block';
        }
        return this.data?.block.contentTypeKey || 'Block';
    }

    #selectTab(group: BlockGroup) {
        this._selectedGroup = group;
    }

    #renderEditors(editors: BlockProperties) {
        return Object.entries(editors).map(([key, property]) => {
            const propertyEditorScheme = umbExtensionsRegistry.getByAlias<ManifestPropertyEditorSchema>(property.alias);
            if (!propertyEditorScheme) {
                console.error(`Property editor with alias ${property.alias} not found.`);
                return nothing;
            }

            return html`
                    <umb-property
                        .label=${property.label || ''}
                        .alias=${key}
                        .description=${property.description || ''}
                        .config=${property.configurations}
                        .appearance=${{labelOnTop: false}}
                        .propertyEditorUiAlias=${propertyEditorScheme.meta.defaultPropertyEditorUiAlias}
                        .validation=${property.validation}
                    >
                    </umb-property>
            `;
        });
    }

    #renderGroups(groups: BlockGroup[]) : any{
        return repeat(groups, group => group.alias, group => html`
            <uui-box headline=${group.label ?? group.alias} >
                ${group.editors && Object.keys(group.editors).length > 0 ? this.#renderEditors(group.editors) : ''}
                ${group.groups && group.groups.length > 0 ? this.#renderGroups(group.groups) : ''}
            </uui-box>
        `);
    }

    render() {
        if (this._loading) {
            return html`
                <umb-body-layout headline=${"Block Properties: " + this.#getBlockTypeName()}>
                    <div>
                        <uui-loader></uui-loader>
                    </div>
                </umb-body-layout>
            `;
        }

        if (this._selectedGroup && (this._selectedGroup.groups?.length ?? 0) == 0 && Object.keys(this._selectedGroup.editors ?? {}).length === 0) {
            return html`
                <umb-body-layout headline=${"Block Properties: " + this.#getBlockTypeName()}>
                    <div>
                        <p>No properties available.</p>
                    </div>
                    <div slot="actions">
                        <uui-button look="secondary" label="Cancel" @click="${this._rejectModal}">
                            Cancel
                        </uui-button>
                        <uui-button look="primary" color="positive" label="Continue" @click="${this._submitProperties}">
                            Continue
                        </uui-button>
                    </div>
                </umb-body-layout>
            `;
        }
        return html`
            <umb-body-layout headline=${"Block Properties: " + this.#getBlockTypeName()}>
                ${this._tabs.length > 0 ? html`<div slot="header">
                    <uui-tab-group dropdownContentDirection="vertical">
                        ${repeat(this._tabs, t => t.alias, tab => 
                            html`
                            <uui-tab @click=${() => this.#selectTab(tab)} .orientation=${'horizontal'} ?active=${tab.alias === this._selectedGroup?.alias}>
                                ${tab.label ?? tab.alias}
                            </uui-tab>
                            `
                        )}
                    </uui-tab-group>
                </div>` : ''}
                <div class="properties-content">
                    <umb-property-dataset 
                    .name=${"properties_dataset"} 
                    .value=${this._properties} 
                    
                    @change=${this.#onDataChange}>
                        ${this._selectedGroup ? html`
                            ${this._selectedGroup.editors && Object.keys(this._selectedGroup.editors).length > 0 ? html`
                                <uui-box>
                                    ${this.#renderEditors(this._selectedGroup.editors)}
                                </uui-box>
                            ` : ''}

                            ${this._selectedGroup.groups && this._selectedGroup.groups.length > 0 ? this.#renderGroups(this._selectedGroup.groups) : ''}
                        ` : ''}

                        <!-- Render the hidden tabs for validation purposes -->
                        ${repeat(this._tabs.filter(tab => tab.alias !== this._selectedGroup?.alias), tab => tab.alias, tab => html`
                            <div style="display: none;">
                                ${tab.editors && Object.keys(tab.editors??{}).length > 0 ? html`
                                    ${this.#renderEditors(tab.editors)}
                                ` : ''}
                                ${tab.groups && tab.groups.length > 0 ? html`
                                    ${this.#renderGroups(tab.groups)}
                                ` : ''}
                            </div>
                        `)}
                    </umb-property-dataset>
                </div>
                <div slot="actions">
                    <uui-button look="secondary" label="Cancel" @click="${this._rejectModal}">
                        Cancel
                    </uui-button>
                    <uui-button look="primary" color="positive" label="Save" @click="${this._submitProperties}">
                        Save Properties
                    </uui-button>
                </div>
            </umb-body-layout>
            <umb-backoffice-notification-container></umb-backoffice-notification-container>
        `;
    }    
    
    private async _submitProperties() {
        this.#validation.validate().then(() => {    
            const result: BlockPropertiesModalResult = {
                properties: this._properties,
                block: this.data?.block,
                action: this.data?.action || 'edit',
                index: this.data?.index,
                uniquePath: this.data?.uniquePath,
            };
            
            this.value = result;
            this._submitModal();
        }, () => {
            this.#notificationContext?.peek('danger', {
                data: {
                    headline: 'Validation Error',
                    message: 'Please resolve validation errors before saving.',
                    duration: 1,
                }
            });
        });
    }

    static styles = css`
        umb-property-layout {
            padding-top: 0;
        }

        :host {
            --uui-tab-divider: var(--uui-color-border);
        }

        umb-property {
            --uui-size-layout-1: 10px;
        }

        umb-property-dataset > umb-property {
            margin-bottom: var(--uui-size-layout-1);
        }

        umb-property-dataset > uui-box {
            margin-bottom: var(--uui-size-layout-1);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'bf-block-properties-modal': BlockPropertiesModalElement;
    }
}
