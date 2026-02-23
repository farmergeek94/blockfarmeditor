import { css, html } from "lit";
import { customElement, state } from "lit/decorators.js";
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import type { ImportModalData, ImportModalResult } from '../tokens/import-modal.token';

@customElement('bf-import-modal')
export class ImportModalElement extends UmbModalBaseElement<ImportModalData, ImportModalResult> {
    @state()
    private _file?: File;

    @state()
    private _overwriteElementTypes: boolean = false;

    @state()
    private _overwriteCompositions: boolean = false;

    @state()
    private _overwriteBlockDefinitions: boolean = false;

    @state()
    private _overwritePartialViews: boolean = false;

    @state()
    private _overwriteDataTypes: boolean = false;

    @state()
    private _fileName: string = '';

    #handleFileChange = (e: Event) => {
        const input = e.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this._file = input.files[0];
            this._fileName = this._file.name;
        } else {
            this._file = undefined;
            this._fileName = '';
        }
    }

    #handleElementTypesToggle = (e: Event) => {
        this._overwriteElementTypes = (e.target as HTMLInputElement).checked;
    }

    #handleCompositionsToggle = (e: Event) => {
        this._overwriteCompositions = (e.target as HTMLInputElement).checked;
    }

    #handleBlockDefinitionsToggle = (e: Event) => {
        this._overwriteBlockDefinitions = (e.target as HTMLInputElement).checked;
    }

    #handlePartialViewsToggle = (e: Event) => {
        this._overwritePartialViews = (e.target as HTMLInputElement).checked;
    }

    #handleOverwriteToggle = (e: Event) => {
        this._overwriteDataTypes = (e.target as HTMLInputElement).checked;
    }

    #clearFile = () => {
        this._file = undefined;
        this._fileName = '';
        // Reset the file input
        const fileInput = this.shadowRoot?.querySelector('input[type="file"]') as HTMLInputElement;
        if (fileInput) {
            fileInput.value = '';
        }
    }

    #handleSubmit = () => {
        const result: ImportModalResult = {
            file: this._file,
            overwriteElementTypes: this._overwriteElementTypes,
            overwriteCompositions: this._overwriteCompositions,
            overwriteBlockDefinitions: this._overwriteBlockDefinitions,
            overwritePartialViews: this._overwritePartialViews,
            overwriteDataTypes: this._overwriteDataTypes
        };

        this.modalContext?.updateValue(result);
        this.modalContext?.submit();
    }

    #closeModal = () => {
        this.modalContext?.reject();
    }

    render() {
        return html`
            <umb-body-layout headline="Import Definitions">
                <div class="modal-content">
                    <p class="description">
                        Import definitions, element types, data types, and partial views from a ZIP file 
                        or from the server's BlockFarmEditor folder.
                    </p>

                    <div class="section">
                        <h3>Import Source</h3>
                        <div class="file-upload-area">
                            <input 
                                type="file" 
                                accept=".zip"
                                @change="${this.#handleFileChange}"
                                id="file-input"
                                class="file-input"
                            />
                            <label for="file-input" class="file-label">
                                <umb-icon name="icon-upload"></umb-icon>
                                <span>Choose ZIP file or drag here</span>
                            </label>
                            ${this._fileName ? html`
                                <div class="selected-file">
                                    <umb-icon name="icon-zip"></umb-icon>
                                    <span>${this._fileName}</span>
                                    <uui-button 
                                        look="secondary" 
                                        compact 
                                        @click="${this.#clearFile}"
                                    >
                                        <umb-icon name="icon-delete"></umb-icon>
                                    </uui-button>
                                </div>
                            ` : ''}
                        </div>
                        <p class="hint">
                            ${this._file 
                                ? 'Import from uploaded ZIP file' 
                                : 'No file selected - will import from BlockFarmEditor folder on server'}
                        </p>
                    </div>

                    <div class="section">
                        <h3 style="margin-bottom: 0">Overwrite Options</h3>
                        <p class="hint">New items are always imported. These options control whether to update existing items.</p>
                        
                        <div class="option-row">
                            <uui-toggle 
                                .checked="${this._overwriteElementTypes}"
                                @change="${this.#handleElementTypesToggle}"
                                label="Overwrite existing element types"
                            ></uui-toggle>
                        </div>
                        
                        <div class="option-row">
                            <uui-toggle 
                                .checked="${this._overwriteCompositions}"
                                @change="${this.#handleCompositionsToggle}"
                                label="Overwrite existing compositions"
                            ></uui-toggle>
                        </div>
                        
                        <div class="option-row">
                            <uui-toggle 
                                .checked="${this._overwriteBlockDefinitions}"
                                @change="${this.#handleBlockDefinitionsToggle}"
                                label="Overwrite existing block definitions"
                            ></uui-toggle>
                        </div>
                        
                        <div class="option-row">
                            <uui-toggle 
                                .checked="${this._overwritePartialViews}"
                                @change="${this.#handlePartialViewsToggle}"
                                label="Overwrite existing partial views"
                            ></uui-toggle>
                        </div>
                        
                        <div class="option-row">
                            <uui-toggle 
                                .checked="${this._overwriteDataTypes}"
                                @change="${this.#handleOverwriteToggle}"
                                label="Overwrite existing data types"
                            ></uui-toggle>
                        </div>
                    </div>
                </div>

                <div slot="actions">
                    <uui-button look="secondary" label="Cancel" @click="${this.#closeModal}">Cancel</uui-button>
                    <uui-button look="primary" label="Import" @click="${this.#handleSubmit}">
                        Import
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = css`
        .modal-content {
            padding: var(--uui-size-space-4);
        }

        .description {
            margin: 0 0 var(--uui-size-space-5) 0;
            color: var(--uui-color-text-alt);
        }

        .section {
            margin-bottom: var(--uui-size-space-5);
        }

        h3 {
            margin: 0 0 var(--uui-size-space-3) 0;
            font-size: var(--uui-type-h5-size);
            font-weight: 500;
            color: var(--uui-color-text);
        }

        h4 {
            margin: 0 0 var(--uui-size-space-2) 0;
            font-size: var(--uui-type-default-size);
            font-weight: 500;
        }

        .file-upload-area {
            position: relative;
            margin-bottom: var(--uui-size-space-3);
        }

        .file-input {
            position: absolute;
            width: 100%;
            height: 100%;
            opacity: 0;
            cursor: pointer;
            z-index: 1;
        }

        .file-label {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: var(--uui-size-space-6);
            border: 2px dashed var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background-color: var(--uui-color-surface-alt);
            cursor: pointer;
            transition: all 0.2s ease;
        }

        .file-label:hover {
            border-color: var(--uui-color-border-emphasis);
            background-color: var(--uui-color-surface-emphasis);
        }

        .file-label umb-icon {
            font-size: 32px;
            margin-bottom: var(--uui-size-space-2);
            color: var(--uui-color-text-alt);
        }

        .file-label span {
            color: var(--uui-color-text-alt);
        }

        .selected-file {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            margin-top: var(--uui-size-space-3);
            padding: var(--uui-size-space-3);
            background-color: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }

        .selected-file umb-icon {
            color: var(--uui-color-positive);
        }

        .selected-file span {
            flex: 1;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .hint {
            margin: 0;
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
        }

        .option-row {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-2);
        }

        .info-box {
            margin-top: var(--uui-size-space-4);
            padding: var(--uui-size-space-3);
            background-color: var(--uui-color-surface-alt);
            border-radius: var(--uui-border-radius);
            border: 1px solid var(--uui-color-border);
        }

        .info-box ul {
            margin: 0;
            padding-left: var(--uui-size-space-5);
        }

        .info-box li {
            margin-bottom: var(--uui-size-space-1);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'bf-import-modal': ImportModalElement;
    }
}
