import { UMB_ACTION_EVENT_CONTEXT, UmbActionEventContext } from '@umbraco-cms/backoffice/action'
import { UMB_AUTH_CONTEXT, UmbAuthContext } from '@umbraco-cms/backoffice/auth'
import { UMB_DOCUMENT_TYPE_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document-type'
import { UmbRequestReloadStructureForEntityEvent } from '@umbraco-cms/backoffice/entity-action'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { UMB_MODAL_MANAGER_CONTEXT, UmbModalManagerContext } from '@umbraco-cms/backoffice/modal'
import { UMB_NOTIFICATION_CONTEXT, UmbNotificationContext } from '@umbraco-cms/backoffice/notification'
import { UMB_PARTIAL_VIEW_PICKER_MODAL } from '@umbraco-cms/backoffice/partial-view'
import { css, html } from 'lit'
import { customElement, state } from 'lit/decorators.js'
import { UrlHelper } from '../../helpers/UrlHelper';

/**
 * Block Farm Editor Definition Form
 */
@customElement('blockfarmeditor-definitions')
export class DefinitionsWorkspace extends UmbLitElement {
  private authContext?: UmbAuthContext
  private moduleManager?: UmbModalManagerContext;
  private notificationContext?: UmbNotificationContext;
  #eventContext?: UmbActionEventContext;
  constructor() {
    super()
    this._onSubmit = this._onSubmit.bind(this);
    this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
      this.authContext = authContext
    })
    // [TODO: Remove this when the UmbActionEventContext is available in the context]
    this.consumeContext(UMB_ACTION_EVENT_CONTEXT , (actionEventContext) => {
      this.#eventContext = actionEventContext;

      this.#eventContext?.removeEventListener(
				UmbRequestReloadStructureForEntityEvent.TYPE,
				this._onSubmit,
			);

      this.#eventContext?.addEventListener(
        UmbRequestReloadStructureForEntityEvent.TYPE, 
        this._onSubmit
      );
    })

    this.consumeContext(UMB_DOCUMENT_TYPE_WORKSPACE_CONTEXT, (instance) => {
      this.observe(instance?.data, (documentType) => {
        if (documentType && this.authContext) {
          this.fetchFormData(documentType.alias);
        }
      })
    })
    this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (modalManager) => {
      this.moduleManager = modalManager;
    })
    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (instance) => {
        this.notificationContext = instance;
    });
  }

  @state()
  private formData = {
    id: -1,
    key: '',
    contentTypeAlias: '',
    type: 'partial',
    viewPath: '',
    category: '',
    enabled: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    createdBy: '',
    updatedBy: ''
  }

  @state()
  private categories: string[] = []

  @state()
  private newCategory = false

  @state()
  private isEditing = false

  @state()
  private saving = false

  fetchFormData(documentType: string) {
    this.authContext?.getLatestToken().then((token) => {
      return fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions?alias=${documentType}`, {
        headers: { Authorization: `Bearer ${token}` }
      }).then(response => response.json()).then(json => {
        if (json && json.data) {
          this.formData = json.data;
          this.isEditing = true;
        } else {
          this.formData = { ...this.formData, contentTypeAlias: documentType };
        }
        this.fetchCategories();
      })
    }).catch(error => {
      this.notificationContext?.peek("danger", {
        data: {
          message: error.message || 'An error occurred while fetching the definition.',
          headline: 'Error fetching definition'
        }
      });
      this.formData = { ...this.formData, contentTypeAlias: documentType };
    });
  }

  fetchCategories() {
    this.authContext?.getLatestToken().then((token) => {
      return fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions/categories`, {
        headers: { Authorization: `Bearer ${token}` }
      }).then(response => response.json()).then(json => {
        if (json && json.data) {
          this.categories = json.data;
          this.newCategory = !this.categories.includes(this.formData.category);
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

  updateDefinition() {
    if (this.authContext) {
      this.authContext.getLatestToken().then((token) => {
        fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions/update/${this.formData.id}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`
          },
          body: JSON.stringify(this.formData)
        }).then(response => response.json()).then(json => {
          if (json && json.data) {
            this.formData = json.data;
            this.saving = false;
            this.fetchCategories();
            this.notificationContext?.peek("positive", {
              data: {
                message: 'Definition updated successfully.',
                headline: 'Success'
              }
            });
          }
        })
      })
    }
  }

  createDefinition() {
    if (this.authContext) {
      this.authContext.getLatestToken().then((token) => {
        fetch(`${UrlHelper.getBaseUrl()}/umbraco/blockfarmeditor/definitions/create`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`
          },
          body: JSON.stringify(this.formData)
        }).then(response => response.headers.get("location")).then(location => {
          if (location) {
            return fetch(location, {
              headers: { Authorization: `Bearer ${token}` }
            }).then(response => response.json()).then(json => {
              if (json && json.data) {
                this.formData = json.data;
                this.isEditing = true;
                this.saving = false;
                this.fetchCategories();
                this.notificationContext?.peek("positive", {
                  data: {
                    message: 'Definition created successfully.',
                    headline: 'Success'
                  }
                });
              }
            });
          }
        }).catch(error => {
          this.notificationContext?.peek("danger", {
            data: {
              message: error.message || 'An error occurred while creating the definition.',
              headline: 'Error creating definition'
            }
          });
          this.isEditing = false;
          this.saving = false;
        });
      })
    }
  }

  private async openFileBrowser() {
    // Use Umbraco's file picker or create custom modal
    if (!this.moduleManager) {
      return;
    }
    const modal = this.moduleManager.open(this, UMB_PARTIAL_VIEW_PICKER_MODAL, {
      data: {
        pickableFilter: (item) => !item.isFolder,
      }
    });

    const result = await modal.onSubmit().catch(() => undefined);
    if (result && result.selection.length > 0 && result.selection[0]) {
      var selectedItem = decodeURIComponent(result.selection[0]).replace("%dot%", ".");
      this._updateFormData('viewPath', "~/Views/Partials" + selectedItem);
    }
  }

  render() {
    return html`
      <umb-body-layout>
      <uui-box header="Block Farm Editor Definition" description="Create or edit a Block Farm Editor definition.">
        <umb-property-layout
          name="Content Type Alias"
          description="The alias of the content type."
          orientation="horizontal"
          label="Content Type Alias"
          width="full"
        >
          <div slot="editor">
            <uui-input
              id="contentTypeAlias"
              .value=${this.formData.contentTypeAlias}
              placeholder="Content type alias" disabled>
            </uui-input>
          </div>
      </umb-property-layout>
      <umb-property-layout
        name="Type"
        description="The type of the definition."
        orientation="horizontal"
        label="Type"
        ?mandatory=${true}
        width="full"
      >
        <div slot="editor">
          <uui-select
            id="type"
            @change=${(e: Event) => this._updateFormData('type', (e.target as HTMLSelectElement).value)}
            placeholder="Select type"
            .options=${[
              { name: 'Partial', value: 'partial', selected: this.formData.type === 'partial' },
              { name: 'View Component', value: 'viewcomponent', selected: this.formData.type === 'viewcomponent' }
            ]}
            required>
          </uui-select>
          ${this.formData.type === 'viewcomponent' ? html`
            <p class="info-text">Register a View Component for your view.</p>
          ` : ''}
        </div>
      </umb-property-layout>

            
            ${this.formData.type === "partial" ? html`
              <umb-property-layout
                name="View Path"
                description="The path to the partial view file."
                orientation="horizontal"
                label="View Path"
                ?mandatory=${true}
                width="full"
              >
                <div slot="editor">
                  <div class="view-path-input">
                    <uui-input
                      id="viewPath"
                      .value=${this.formData.viewPath}
                      @input=${(e: Event) => this._updateFormData('viewPath', (e.target as HTMLInputElement).value)}
                      placeholder="~/Views/Partials/..."
                      required>
                    </uui-input>
                    <uui-button type="button" @click=${this.openFileBrowser} color="default" look="primary">
                      Select View
                    </uui-button>
                  </div>
                </div>
              </umb-property-layout>
          ` : ''}

          <umb-property-layout
            name="Category"
            description="The category of the definition."
            orientation="horizontal"
            label="Category"
            ?mandatory=${true}
            width="full"
          >
            <div slot="editor">
              <uui-select
                id="category"
                @change=${(e: Event) => this._updateFormData('category', (e.target as HTMLSelectElement).value)}
                placeholder="Select category"
                .options=${[
        {
          name: 'New Category',
          value: '',
          selected: this.newCategory
        },
        ...this.categories.map(category => ({ name: category, value: category, selected: this.formData.category === category && !this.newCategory }))
      ]}
                required>
              </uui-select>
              ${this.newCategory ?
        html`
              <uui-input
                class="new-category-input"
                id="category"
                .value=${this.formData.category}
                @input=${(e: Event) => this._updateFormData('category', (e.target as HTMLInputElement).value)}
                placeholder="Enter category"
                maxlength="255"
                >
              </uui-input>
          ` : ''}
            </div>
          </umb-property-layout>

          <umb-property-layout
            name="Enabled"
            description="Toggle to enable or disable the definition."
            orientation="horizontal"
            label="Enabled"
          >
          <div slot="editor">
              <uui-checkbox
                .checked=${this.formData.enabled}
                @change=${(e: Event) => this._updateFormData('enabled', (e.target as HTMLInputElement).checked)}
                pristine>
              </uui-checkbox>
          </div>
          </umb-property-layout>
      </uui-box><!-- 
      <div slot="actions">
            <uui-button type="button" color="positive" look="primary" @click=${this._onSubmit}>
              ${this.saving ? html`<uui-loader-circle style="color: white"></uui-loader-circle>` : ''}
              ${!this.saving ? html`${this.isEditing ? 'Update' : 'Create'} Definition` : ''}
            </uui-button>
      </div> -->
      </umb-body-layout>
    `
  }

  private _updateFormData(field: string, value: any) {
    if (field === 'category') {
      if(this.categories.includes(value)) {
        this.newCategory = false;
      } else {
        this.newCategory = true;
      }
    }
    this.formData = {
      ...this.formData,
      [field]: value,
      updatedAt: new Date().toISOString()
    }
  }

  private _onSubmit() {
    this.saving = true;
    if (this.formData.id > 0) {
      this.updateDefinition()
    } else {
      this.createDefinition()
    }
  }

  static styles = css`
    .view-path-input {
      display: flex;
      gap: 0.5rem;
    }

    .view-path-input uui-input {
      flex: 1;
      width: auto;
    }

    .new-category-input {
      margin-top: 0.5rem;
    }

    .form-section {
      margin-bottom: 2rem;
      padding-bottom: 1.5rem;
    }

    .form-section:not(:last-child) {
      border-bottom: 1px solid var(--uui-color-border);
    }

    .readonly-section {
      background: var(--uui-color-surface-alt);
      padding: 1.5rem;
      border-radius: var(--uui-border-radius);
    }

    uui-input,
    uui-select {
      width: 100%;
    }

    .form-field {
      margin-bottom: 1.5rem;
    }

    .form-field:last-child {
      margin-bottom: 0;
    }
  `
}

declare global {
  interface HTMLElementTagNameMap {
    'blockfarmeditor-definitions': DefinitionsWorkspace
  }
}
