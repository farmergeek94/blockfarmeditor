import { LitElement } from "lit";
import { property } from "lit/decorators.js";


/**
 * An example element.
 *
 * @slot - This element has a slot
 * @csspart button - The button
 */
export class BlockElement extends LitElement {        
    /**
     * Builds the identifier path by traversing up the DOM to find all parent 
     * block elements and concatenating their identifiers
     */
    private buildIdentifierPath(): void {
        let path: string[] = []
        let parentElement: HTMLElement | null = this.parentElement
        // Traverse up the DOM tree to find all parent block elements
        // and add their unique identifiers to the path
        while (parentElement) {
            if (
                parentElement.tagName.toLowerCase() === 'block-item' ||
                parentElement.tagName.toLowerCase() === 'block-area'
            ) {
                const unique = (parentElement as any).unique;
                if (unique) {
                path.unshift(unique);
                }
            }

            // Traverse up through shadow DOM hosts if present
            const root = parentElement.getRootNode();
            if (root instanceof ShadowRoot) {
                parentElement = (root as ShadowRoot).host as HTMLElement;
            } else {
                // Otherwise, go up the light DOM
                parentElement = parentElement.parentElement;
            }
        }

        path.unshift("64D51B94-10C1-4225-9329-B2A54D0E47CE")
        
        // Add the current element's identifier to the path
        if (this.unique) {
            path.push(this.unique)
        }
        
        // Join all identifiers with a separator
        this.uniquePath = path.join('/')
    }
        
    connectedCallback(): void {
        super.connectedCallback()
        // Rebuild the path in case DOM has changed
        this.buildIdentifierPath()
    }

    disconnectedCallback(): void {
        super.disconnectedCallback()
    }

    // Override the method to prevent creating a shadow DOM
    // This is important for the block element to work correctly
    protected createRenderRoot(): HTMLElement | DocumentFragment {
        return this;
    }

    @property({ type: String })
    public unique = ''

    @property({ type: String })
    public uniquePath = ''
    
    @property({ type: String })
    public allowedblocks?:string;
}