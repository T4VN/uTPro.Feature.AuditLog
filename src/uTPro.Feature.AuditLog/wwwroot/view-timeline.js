import { UtproAuditLogDashboard } from './index.js';

export default class UtproAuditLogTimelineView extends UtproAuditLogDashboard {
    constructor() {
        super();
        this.activeTab = 'timeline';
    }
}

customElements.define('utpro-audit-log-timeline', UtproAuditLogTimelineView);
