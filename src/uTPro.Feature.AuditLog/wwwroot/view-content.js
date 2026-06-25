import { UtproAuditLogDashboard } from './index.js';

export default class UtproAuditLogContentView extends UtproAuditLogDashboard {
    constructor() {
        super();
        this.activeTab = 'log';
    }
}

customElements.define('utpro-audit-log-content', UtproAuditLogContentView);
