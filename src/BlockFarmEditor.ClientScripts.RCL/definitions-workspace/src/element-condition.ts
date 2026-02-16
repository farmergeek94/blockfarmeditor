import { UMB_DOCUMENT_TYPE_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document-type';
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import type { UmbConditionControllerArguments, UmbExtensionCondition } from '@umbraco-cms/backoffice/extension-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export const UMB_WORKSPACE_IS_ELEMENT_CONDITION_ALIAS = 'BlockFarmEditor.Condition.IsElement';

export default class UmbWorkspaceIsElementCondition
    extends UmbConditionBase<any>
    implements UmbExtensionCondition {
    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<any>) {
        super(host, args);
        this.consumeContext(UMB_DOCUMENT_TYPE_WORKSPACE_CONTEXT, (context) => {
            if (!context && args.config.match === false) {
                this.permitted = true;
                return;
            } else if (!context) {
                this.permitted = false;
                return;
            }
            this.observe(context.isElement, (isElement) => {
                if (args.config.match === false) {
                    this.permitted = (isElement ?? false) === false;
                } else {
                    this.permitted = isElement ?? false;
                }
            });
        });
    }
}