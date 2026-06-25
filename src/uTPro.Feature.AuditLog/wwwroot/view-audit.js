import { UtproAuditLogDashboard } from './index.js';

export default class UtproAuditLogAuditView extends UtproAuditLogDashboard {
    constructor() {
        super();
        this.activeTab = 'audit';
    }
}

customElements.define('utpro-audit-log-audit', UtproAuditLogAuditView);
