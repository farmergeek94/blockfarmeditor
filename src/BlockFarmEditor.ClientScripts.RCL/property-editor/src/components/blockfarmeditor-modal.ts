import { css, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import type { BlockFarmEditorModalData, BlockFarmEditorModalResult } from '../tokens/blockfarm-editor-modal.token';
import { PreviewService } from '@umbraco-cms/backoffice/external/backend-api';
import { BLOCKFARMEDITOR_PROPERTIES_MODAL, type BlockPropertiesModalData, type BlockPropertiesModalResult } from '../tokens/block-properties-modal.token';
import { BLOCKFARMEDITOR_ADD_BLOCK_MODAL, type AddBlockModalData, type AddBlockModalResult } from '../tokens/add-block-modal.token';
import { UmbModalRouteRegistrationController } from '@umbraco-cms/backoffice/router';
import { BLOCKFARMEDITOR_USE_LAYOUT_MODAL, type UseLayoutModalData, type UseLayoutModalResult } from '../tokens/use-layout-modal.token';
import { BLOCKFARMEDITOR_SAVE_LAYOUT_MODAL, type SaveLayoutModalData } from '../tokens/save-layout-modal.token';
import { UMB_NOTIFICATION_CONTEXT, UmbNotificationContext } from '@umbraco-cms/backoffice/notification'

/**
 * BlockFarmEditor modal component with iframe
 */
@customElement('bf-blockfarm-editor-modal')
export class BlockFarmEditorModal extends UmbModalBaseElement<BlockFarmEditorModalData, BlockFarmEditorModalResult> {
    
    private _blockPropertiesModalRegistration: UmbModalRouteRegistrationController<BlockPropertiesModalData, BlockPropertiesModalResult>;
    private _addBlockModalRegistration: UmbModalRouteRegistrationController<AddBlockModalData, AddBlockModalResult>;
    private _useLayoutModalRegistration: UmbModalRouteRegistrationController<UseLayoutModalData, UseLayoutModalResult>;
    private _saveLayoutModalRegistration: UmbModalRouteRegistrationController<SaveLayoutModalData>;
    private notificationContext?: UmbNotificationContext;

    constructor() {
        super();
        this._handleIframeMessage = this._handleIframeMessage.bind(this);
        this._addBlockModalRegistration = new UmbModalRouteRegistrationController(
            this,
            BLOCKFARMEDITOR_ADD_BLOCK_MODAL
        )
        .onSubmit((result: AddBlockModalResult) => {
            if (result) {
                this._processAddBlockResult(result);
            }
        });

        this._blockPropertiesModalRegistration = new UmbModalRouteRegistrationController(
            this,
            BLOCKFARMEDITOR_PROPERTIES_MODAL
        )
        .onSubmit((result: BlockPropertiesModalResult) => {
            if (result && result.uniquePath) {
                this._processBlockPropertiesResult(result);
            }
        });

        this._useLayoutModalRegistration = new UmbModalRouteRegistrationController(
            this,
            BLOCKFARMEDITOR_USE_LAYOUT_MODAL
        )
        .onSubmit((result: UseLayoutModalResult) => {
            if (result) {
                this._processUseLayoutResult(result);
            }
        });

        this._saveLayoutModalRegistration = new UmbModalRouteRegistrationController(
            this,
            BLOCKFARMEDITOR_SAVE_LAYOUT_MODAL
        );
        
      
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (instance) => {
            this.notificationContext = instance;
        });
    }

    @state()
    private _iframeLoaded: boolean = false;

    @state()
    private contentUnique?: string;

    async firstUpdated() {
        await PreviewService.postPreview();
        this.contentUnique = this.data?.contentUnique;
    }

    connectedCallback() {
        super.connectedCallback();
        window.addEventListener('message', this._handleIframeMessage);
    }
    disconnectedCallback() {
        window.removeEventListener('message', this._handleIframeMessage);
        this._blockPropertiesModalRegistration?.destroy();
        this._addBlockModalRegistration?.destroy();
        super.disconnectedCallback();
    }

    queryString() {
        const params = new URLSearchParams();
        if (this.data?.culture) {
            params.set('culture', this.data.culture ?? 'default');
        }
        params.set('preview', 'true');
        params.set('editmode', 'true');
        return params.toString();
    }

    private _requestShowLayers() {
        const iframe = this.shadowRoot?.querySelector('.block-farm-iframe') as HTMLIFrameElement;

        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                messageType: 'blockfarmeditor:show-layers',
            }, window.location.origin);
        }
    }
    private _handleIframeLoad(event: Event) {
        const iframe = event.target as HTMLIFrameElement;

        if (iframe && iframe.contentWindow) {
            this._iframeLoaded = true; iframe.contentWindow.postMessage({
                messageType: 'blockfarmeditor:initialize',
                data: { ...this.value },
                culture: this.data?.culture
            }, window.location.origin);
        }
    }

    private _saveChanges() {
        const iframe = this.shadowRoot?.querySelector('.block-farm-iframe') as HTMLIFrameElement;

        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                messageType: 'blockfarmeditor:page-updated'
            }, window.location.origin);
        } else {
            console.error('Unable to access iframe content window');
        }
    }

    private _handleIframeMessage(event: MessageEvent) {
        if (event.origin !== window.location.origin) {
            console.warn('Received message from unknown origin:', event.origin);
            return;
        }
        if (!event.data) {
            console.warn('Received message without data:', event);
            return;
        } if (event.data.messageType === 'blockfarmeditor:page-updated') {
            this.value = event.data.pageDefinition;
            this._submitModal();
        } else if (event.data.messageType === 'blockfarmeditor:block-properties') {
            this._handleBlockPropertiesMessage(event.data);
        } else if (event.data.messageType === 'blockfarmeditor:add-block') {
            this._handleAddBlockMessage(event.data);
        } else if (event.data.messageType === 'blockfarmeditor:use-layout') {
            this._handleUseLayoutMessage(event.data);
        } else if( event.data.messageType == 'blockfarmeditor:save-layout') {
            this._handleSaveLayoutMessage(event.data);
        } else if (event.data.messageType === 'blockfarmeditor:message') {
            this.notificationContext?.peek(event.data.color, {
                data: {
                    message: event.data.message,
                    headline: event.data.headline,
                }
            });
        }
    }


    private async _handleBlockPropertiesMessage(messageData: any) {
        // Update the setup data for the existing registration
        this._blockPropertiesModalRegistration.onSetup(() => ({
            data: {
                block: messageData.block,
                uniquePath: messageData.uniquePath,
                action: messageData.action,
                index: messageData.index,
            }, 
            value: {
                properties: [],
                action: "edit",
            }
        }));

        // Open the modal
        this._blockPropertiesModalRegistration.open({});
    }

    private _processBlockPropertiesResult(result: BlockPropertiesModalResult) {
        const blockUniquePath = result.uniquePath?.split('/').slice(0, -1).join('/');
        const propertiesArray = result.properties;
        const properties: { [key: string]: any } = {};

        for (const item of propertiesArray) {
            properties[item.alias] = item.value;
        }

        const iframe = this.shadowRoot?.querySelector('.block-farm-iframe') as HTMLIFrameElement;

        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                messageType: 'blockfarmeditor:properties-updated',
                uniquePath: result.uniquePath,
                blockUniquePath,
                block: { ...result.block, properties },
                index: result.index,
                action: result.action
            }, window.location.origin);
        }
    }

    private async _handleAddBlockMessage(messageData: any) {
        // Create modal registration if it doesn't exist
        // Update the setup data for the existing registration
        const modalData: AddBlockModalData = {
            uniquePath: messageData.uniquePath,
            index: messageData.index,
            allowedBlocks: messageData.allowedblocks
        };
        this._addBlockModalRegistration.onSetup(() => ({
            data: modalData,
            value: {} as any
        }));

        // Open the modal
        this._addBlockModalRegistration.open({});
    }
    private _processAddBlockResult(result: AddBlockModalResult) {
        const iframe = this.shadowRoot?.querySelector('.block-farm-iframe') as HTMLIFrameElement;

        if (iframe && iframe.contentWindow) {



            iframe.contentWindow.postMessage({
                messageType: 'blockfarmeditor:block-added',
                block: result.block,
                uniquePath: result.uniquePath,
                index: result.index
            }, window.location.origin);
        }
    }

    private async _handleUseLayoutMessage(messageData: any) {
        // Create modal registration if it doesn't exist
        // Update the setup data for the existing registration
        const modalData: UseLayoutModalData = {
            uniquePath: messageData.uniquePath,
            index: messageData.index,
            fullLayout: messageData.fullLayout
        };
        this._useLayoutModalRegistration.onSetup(() => ({
            data: modalData,
            value: {} as any
        }));

        // Open the modal
        this._useLayoutModalRegistration.open({});
    }

    private _processUseLayoutResult(result: UseLayoutModalResult) {
        const iframe = this.shadowRoot?.querySelector('.block-farm-iframe') as HTMLIFrameElement;
        console.log('Using layout result:', result);
        if (iframe && iframe.contentWindow) {
            iframe.contentWindow.postMessage({
                messageType: 'blockfarmeditor:use-layout-selected',
                block: result.block,
                pageDefinition: result.pageDefinition,
                uniquePath: result.uniquePath,
                index: result.index,
                fullLayout: result.fullLayout
            }, window.location.origin);
        }
    }

    private async _handleSaveLayoutMessage(messageData: any) {
        // Create modal registration if it doesn't exist
        // Update the setup data for the existing registration

        console.log("Handling save layout message", messageData);

        const modalData: SaveLayoutModalData = {
            uniquePath: messageData.uniquePath,
            parentPath: messageData.parentPath,
            layout: messageData.layout,
            fullLayout: messageData.fullLayout
        };
        this._saveLayoutModalRegistration.onSetup(() => ({
            data: modalData,
            value: {} as any
        }));

        // Open the modal
        this._saveLayoutModalRegistration.open({});
    }

    render() {
        return html`
      <umb-body-layout>
            <div slot="header" class="modal-header">
                <h3 class="headline">Block Farm Editor</h3>
            </div>
            <div slot="navigation" class="modal-navigation">
              <uui-button
                look="outline"
                label="Show Layers" 
                @click=${this._requestShowLayers}>
                  Show Layers
              </uui-button>
              <uui-button
                look="secondary"
                label="Cancel" 
                @click=${this._rejectModal}>
                  Cancel
              </uui-button>
              <uui-button
                look="primary"
                label="Save" 
                @click=${this._saveChanges}>
                  Save
              </uui-button>
            </div>
          
          <div class="modal-body">
            ${this.contentUnique ?
                html`
                ${!this._iframeLoaded ?
                        html`<div class="loading-indicator">Loading Block Farm...</div>` :
                        ''
                    }
                <iframe 
                    src="/${this.contentUnique}?${this.queryString()}" 
                    class="block-farm-iframe"
                    @load=${this._handleIframeLoad}>
                  </iframe>` :
                html`<div>Content ID not available</div>`
            }
          </div>
      </umb-body-layout>
    `;
    }
    static styles = css`  
    umb-body-layout {
        --uui-size-layout-1: 0;
    }
    .modal-header {
      padding-left: 24px;
    }

    .modal-navigation {
        padding-right: 24px;
    }
    
    .modal-body {
      overflow: hidden;
      position: relative;
        display: flex;
        flex-direction: column;
        height: 100%;
    }
  
    .block-farm-iframe {
      width: 100%;
      height: 100%;
      border: none;
    }
    
    .loading-indicator {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      z-index: 1;
    }

    .block-farm-iframe.loading {
      opacity: 0.5;
    }
  `;
}

declare global {
    interface HTMLElementTagNameMap {
        'bf-blockfarm-editor-modal': BlockFarmEditorModal
    }
}