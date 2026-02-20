import '../../helpers/UrlHelper.ts';

import { css, html } from 'lit'
import { customElement, property } from 'lit/decorators.js'
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor'
import { PageDefinition } from '../../models/PageDefinition';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT, type UmbDocumentDetailModel, type UmbDocumentWorkspaceContext } from '@umbraco-cms/backoffice/document';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { BLOCKFARMEDITOR_MODAL, type BlockFarmEditorModalData, type BlockFarmEditorModalResult } from './tokens/blockfarm-editor-modal.token.ts';
import { UmbModalRouteRegistrationController } from '@umbraco-cms/backoffice/router';
import { UMB_VARIANT_CONTEXT } from '@umbraco-cms/backoffice/variant';
import { UMB_DOCUMENT_BLUEPRINT_WORKSPACE_CONTEXT, type UmbDocumentBlueprintWorkspaceContext  } from '@umbraco-cms/backoffice/document-blueprint';

/**
 * An example element.
 *
 * @slot - This element has a slot
 * @csspart button - The button
 */
@customElement('blockfarmeditor-page-propertyeditor')
export class BlockFarmEditorPropertyEditor extends UmbLitElement implements UmbPropertyEditorUiElement {
  #modalRegistration: UmbModalRouteRegistrationController<BlockFarmEditorModalData, BlockFarmEditorModalResult>;

  #documentWorkspaceContext?: UmbDocumentWorkspaceContext;
  #documentBlueprintWorkspaceContext?: UmbDocumentBlueprintWorkspaceContext;
  #documentData?: UmbDocumentDetailModel;
  #languageData?: string;
  #isNew?: boolean;
  #isBlueprint?: boolean;
  

  constructor() {
    super();
    this.consumeContext(UMB_DOCUMENT_BLUEPRINT_WORKSPACE_CONTEXT, (instance) => {
      this.#documentBlueprintWorkspaceContext = instance;
      this.observe(this.#documentBlueprintWorkspaceContext?.data, (hasData) => {
        this.#isBlueprint = hasData ? true : false;
      });
    });

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (instance) => {
      this.#documentWorkspaceContext = instance;
      this.observe(this.#documentWorkspaceContext?.data, (document) => {
        this.#documentData = document;
      });
      this.observe(this.#documentWorkspaceContext?.isNew, (isNew) => {
        this.#isNew = isNew;
      });
    });

    console.log("langauge Consume")
    this.consumeContext(UMB_VARIANT_CONTEXT, (instance) => {
      console.log("this is a language", instance)
        this.observe(instance?.culture, (culture) => {
            this.#languageData = culture!;
      });
    });

    this.#modalRegistration = new UmbModalRouteRegistrationController(
      this,
      BLOCKFARMEDITOR_MODAL
    )
      .onSetup(() => ({
        data: {
          value: this.value,
          contentUnique: this.#documentData?.unique,
          culture: this.#languageData
        },
        value: this.value as PageDefinition
      }))
      .onSubmit((result) => {
        if (!result) {
          return;
        }
        this.value = result;
      });
  }

  /**
   * The value of the property editor.
   * This is the data that will be saved to the database.
   * @type {PageDefinition}
   * @memberof BlockFarmEditorPropertyEditor
   * @property {PageDefinition} value
   * @property {string} value.unique - The unique identifier of the page.
   * @property {string} value.type - The identifier of the page.
   * @property {BlockDefinition[]} value.blocks - The blocks of the page.
   */
  @property({ type: Object })
  public value?: PageDefinition;
  // Create element id for this instance
  elementId = `blockfarmeditor-modal-${Math.random().toString(36).substring(2, 10)}`;

  override  render() {
    return html`
      <div class="container">
        ${!(this.#isNew ?? this.#isBlueprint ?? false) ? 
          html`<uui-button
            look="primary"
            label="Editor" 
            @click=${() => this.#modalRegistration?.open({})}>
              Open Block Farm Editor
          </uui-button>` : html`<uui-form-layout-item>
            <div slot="message">${this.#isBlueprint ? "Block Farm Editor is not available for blueprints." : "Block Farm Editor will be available after initial save."}</div> 
          </uui-form-layout-item>`
        } 
      </div>
    `
  }

  // When value changes, dispatch event to notify Umbraco
  updated(changedProperties: Map<string, any>) {
    if (changedProperties.has('value')) {
      this.dispatchEvent(new UmbChangeEvent());
    }
  }

  static styles = css`
    .container {
      padding: 10px;
    }
  `
}

declare global {
  interface HTMLElementTagNameMap {
    'blockfarmeditor-page-propertyeditor': BlockFarmEditorPropertyEditor
  }
}
