// Shared workspace context. Provided once at the Audit Log workspace level so the
// active view and the workspace footer app can coordinate (UTC toggle + CSV export).

import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbBooleanState } from '@umbraco-cms/backoffice/observable-api';

export const UTPRO_AUDIT_LOG_CONTEXT = new UmbContextToken('Utpro.AuditLog.Context');

export class UtproAuditLogContext extends UmbControllerBase {

    #useUtc = new UmbBooleanState(false);
    /** Observable: whether timestamps should be shown in UTC. */
    useUtc = this.#useUtc.asObservable();

    #activeView = null;

    constructor(host) {
        super(host);
        this.provideContext(UTPRO_AUDIT_LOG_CONTEXT, this);
    }

    /** The currently mounted tab view registers itself here. */
    setActiveView(view) {
        this.#activeView = view;
    }

    clearActiveView(view) {
        if (this.#activeView === view) this.#activeView = null;
    }

    setUseUtc(value) {
        this.#useUtc.setValue(value);
    }

    getUseUtc() {
        return this.#useUtc.getValue();
    }

    /** Delegates the export to whichever tab view is currently active. */
    exportCsv() {
        this.#activeView?.exportCsv();
    }
}

export default UtproAuditLogContext;
