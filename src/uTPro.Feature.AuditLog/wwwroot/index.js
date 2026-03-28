import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

const API_BASE = '/umbraco/management/api/v1/utpro/audit-log';

const TAB_ENDPOINTS = {
    timeline: 'timeline',
    audit: 'audit-entries',
    log: 'log-entries'
};

const SEARCH_PLACEHOLDERS = {
    timeline: 'Search user, action, details...',
    audit: 'Search details, user, IP, event type...',
    log: 'Search comment, user, entity, node ID...'
};

export class UtproAuditLogDashboard extends UmbLitElement {

    static properties = {
        activeTab:        { state: true },
        isLoading:        { state: true },
        items:            { state: true },
        totalRecords:     { state: true },
        currentSkip:      { state: true },
        pageSize:         { state: true },
        eventTypes:       { state: true },
        logHeaders:       { state: true },
        users:            { state: true },
        filterEventType:  { state: true },
        filterSearch:     { state: true },
        filterDateFrom:   { state: true },
        filterDateTo:     { state: true },
        filterUserId:     { state: true },
        filterAffectedUserId: { state: true },
        useUtcTime:       { state: true },
    };

    #authContext;

    constructor() {
        super();
        this.activeTab = 'timeline';
        this.isLoading = false;
        this.items = [];
        this.totalRecords = 0;
        this.currentSkip = 0;
        this.pageSize = 20;
        this.eventTypes = [];
        this.logHeaders = [];
        this.users = [];
        this.filterEventType = '';
        this.filterSearch = '';
        this.filterDateFrom = '';
        this.filterDateTo = '';
        this.filterUserId = '';
        this.filterAffectedUserId = '';
        this.useUtcTime = false;
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => {
            this.#authContext = ctx;
        });
    }

    async connectedCallback() {
        super.connectedCallback();
        await this.loadFilterOptions();
        await this.loadData();
    }

    // ── API ──────────────────────────────────────────────

    async fetchApi(endpoint, body = {}) {
        const config = this.#authContext?.getOpenApiConfiguration();
        const headers = { 'Content-Type': 'application/json' };

        if (config?.token) {
            const token = await config.token();
            if (token) headers['Authorization'] = `Bearer ${token}`;
        }

        const response = await fetch(`${API_BASE}/${endpoint}`, {
            method: 'POST',
            headers,
            credentials: config?.credentials || 'same-origin',
            body: JSON.stringify(body)
        });

        if (!response.ok) throw new Error(`API error: ${response.status}`);
        return response.json();
    }

    async loadFilterOptions() {
        try {
            const [eventTypes, logHeaders, users] = await Promise.all([
                this.fetchApi('event-types'),
                this.fetchApi('log-headers'),
                this.fetchApi('users')
            ]);
            this.eventTypes = eventTypes || [];
            this.logHeaders = logHeaders || [];
            this.users = users || [];
        } catch (error) {
            console.error('Failed to load filter options:', error);
        }
    }

    async loadData() {
        this.isLoading = true;
        try {
            const requestBody = {
                skip: this.currentSkip,
                take: this.pageSize
            };

            if (this.filterEventType) requestBody.eventType = this.filterEventType;
            if (this.filterSearch) requestBody.searchTerm = this.filterSearch;
            if (this.filterDateFrom) requestBody.dateFrom = this.filterDateFrom;
            if (this.filterDateTo) requestBody.dateTo = this.filterDateTo;
            if (this.filterUserId) requestBody.userId = parseInt(this.filterUserId);
            if (this.filterAffectedUserId) requestBody.affectedUserId = parseInt(this.filterAffectedUserId);

            const endpoint = TAB_ENDPOINTS[this.activeTab];
            const data = await this.fetchApi(endpoint, requestBody);
            this.items = data.items || [];
            this.totalRecords = data.total || 0;
        } catch (error) {
            console.error('Failed to load data:', error);
            this.items = [];
            this.totalRecords = 0;
        }
        this.isLoading = false;
    }

    // ── Actions ──────────────────────────────────────────

    switchTab(tab) {
        this.activeTab = tab;
        this.currentSkip = 0;
        this.filterEventType = '';
        this.filterSearch = '';
        this.filterDateFrom = '';
        this.filterDateTo = '';
        this.loadData();
    }

    applyFilters() {
        this.currentSkip = 0;
        this.loadData();
    }

    resetFilters() {
        this.filterEventType = '';
        this.filterSearch = '';
        this.filterDateFrom = '';
        this.filterDateTo = '';
        this.filterUserId = '';
        this.filterAffectedUserId = '';
        this.currentSkip = 0;
        this.loadData();
    }

    async exportCsv() {
        try {
            const exportEndpoints = {
                timeline: 'export/timeline',
                audit: 'export/audit-entries',
                log: 'export/log-entries'
            };

            const config = this.#authContext?.getOpenApiConfiguration();
            const headers = { 'Content-Type': 'application/json' };
            if (config?.token) {
                const token = await config.token();
                if (token) headers['Authorization'] = `Bearer ${token}`;
            }

            const requestBody = {};
            if (this.filterEventType) requestBody.eventType = this.filterEventType;
            if (this.filterSearch) requestBody.searchTerm = this.filterSearch;
            if (this.filterDateFrom) requestBody.dateFrom = this.filterDateFrom;
            if (this.filterDateTo) requestBody.dateTo = this.filterDateTo;
            if (this.filterUserId) requestBody.userId = parseInt(this.filterUserId);
            if (this.filterAffectedUserId) requestBody.affectedUserId = parseInt(this.filterAffectedUserId);

            const response = await fetch(`${API_BASE}/${exportEndpoints[this.activeTab]}`, {
                method: 'POST',
                headers,
                credentials: config?.credentials || 'same-origin',
                body: JSON.stringify(requestBody)
            });

            if (!response.ok) throw new Error('Export failed');

            const blob = await response.blob();
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `${this.activeTab}-${new Date().toISOString().slice(0, 10)}.csv`;
            link.click();
            URL.revokeObjectURL(url);
        } catch (error) {
            console.error('Export failed:', error);
        }
    }

    goToPreviousPage() {
        if (this.currentSkip > 0) {
            this.currentSkip = Math.max(0, this.currentSkip - this.pageSize);
            this.loadData();
        }
    }

    goToNextPage() {
        if (this.currentSkip + this.pageSize < this.totalRecords) {
            this.currentSkip += this.pageSize;
            this.loadData();
        }
    }

    // ── Computed ─────────────────────────────────────────

    get currentPage() {
        return Math.floor(this.currentSkip / this.pageSize) + 1;
    }

    get totalPages() {
        return Math.max(1, Math.ceil(this.totalRecords / this.pageSize));
    }

    get isFirstPage() {
        return this.currentSkip === 0;
    }

    get isLastPage() {
        return this.currentSkip + this.pageSize >= this.totalRecords;
    }

    get typeFilterOptions() {
        return this.activeTab === 'audit' ? this.eventTypes
             : this.activeTab === 'log' ? this.logHeaders
             : [];
    }

    get searchPlaceholder() {
        return SEARCH_PLACEHOLDERS[this.activeTab] || 'Search...';
    }

    formatDate(value) {
        if (!value) return '';
        const date = new Date(value);
        return this.useUtcTime
            ? date.toLocaleString(undefined, { timeZone: 'UTC' }) + ' (UTC)'
            : date.toLocaleString();
    }

    formatTimelineDetails(item) {
        const parts = [];

        if (item.source === 'log') {
            if (item.nodeId && item.nodeId > 0) {
                const nodeInfo = item.nodeName
                    ? `#${item.nodeId} - ${item.nodeName}`
                    : `#${item.nodeId}`;
                parts.push(nodeInfo);
            }
            if (item.details) parts.push(item.details);
        } else {
            // audit: details = affectedDetails, extra = eventDetails
            if (item.details) parts.push(item.details);
            if (item.extra) parts.push(item.extra);
        }

        if (parts.length === 0) return '';
        if (parts.length === 1) return parts[0];
        return html`${parts[0]}<br />${parts[1]}`;
    }

    // ── Render ───────────────────────────────────────────

    render() {
        return html`
            <uui-box>
                ${this.renderHeader()}
                ${this.renderFilters()}
                ${this.renderContent()}
                ${this.renderPagination()}
            </uui-box>`;
    }

    renderHeader() {
        return html`
            <div class="header">
                <h2>Audit Log</h2>
                <div class="tabs">
                    <uui-button look=${this.activeTab === 'timeline' ? 'primary' : 'secondary'}
                        @click=${() => this.switchTab('timeline')}>Timeline</uui-button>
                    <uui-button look=${this.activeTab === 'log' ? 'primary' : 'secondary'}
                        @click=${() => this.switchTab('log')}>Content Logs</uui-button>
                    <uui-button look=${this.activeTab === 'audit' ? 'primary' : 'secondary'}
                        @click=${() => this.switchTab('audit')}>Audit Trail</uui-button>
                    <span class="separator"></span>
                    <uui-button look=${this.useUtcTime ? 'primary' : 'outline'} compact
                        @click=${() => { this.useUtcTime = !this.useUtcTime; }}>
                        ${this.useUtcTime ? 'UTC' : 'Local'}
                    </uui-button>
                </div>
            </div>`;
    }

    renderFilters() {
        return html`
            <div class="filters">
                <uui-input
                    placeholder=${this.searchPlaceholder}
                    .value=${this.filterSearch}
                    @input=${(e) => { this.filterSearch = e.target.value; }}
                    @keydown=${(e) => { if (e.key === 'Enter') this.applyFilters(); }}>
                </uui-input>

                <select style="width:150px" class="select" @change=${(e) => { this.filterUserId = e.target.value; }}>
                    <option value="">All Users</option>
                    ${this.users.map((user) => html`
                        <option value=${user.id} ?selected=${this.filterUserId == user.id}>
                            ${user.name} &lt;${user.email}&gt;
                        </option>`)}
                </select>

                ${this.activeTab === 'audit' ? html`
                    <select style="width:150px" class="select" @change=${(e) => { this.filterAffectedUserId = e.target.value; }}>
                        <option value="">All Affected</option>
                        ${this.users.map((user) => html`
                            <option value=${user.id} ?selected=${this.filterAffectedUserId == user.id}>
                                ${user.name} &lt;${user.email}&gt;
                            </option>`)}
                    </select>` : nothing}

                ${this.activeTab !== 'timeline' ? html`
                    <select style="width:150px" class="select" @change=${(e) => { this.filterEventType = e.target.value; }}>
                        <option value="">All Types</option>
                        ${this.typeFilterOptions.map((type) => html`
                            <option value=${type} ?selected=${this.filterEventType === type}>${type}</option>`)}
                    </select>` : nothing}

                <input type="date" class="date-input"
                    .value=${this.filterDateFrom}
                    @change=${(e) => { this.filterDateFrom = e.target.value; }} />
                <input type="date" class="date-input"
                    .value=${this.filterDateTo}
                    @change=${(e) => { this.filterDateTo = e.target.value; }} />

                <uui-button look="primary" @click=${() => this.applyFilters()}>Apply</uui-button>
                <uui-button look="secondary" @click=${() => this.resetFilters()}>Reset</uui-button>
                <span class="filter-spacer"></span>
                <uui-button look="outline" @click=${() => this.exportCsv()}>Export CSV</uui-button>
            </div>`;
    }

    renderContent() {
        if (this.isLoading) {
            return html`<div class="center"><uui-loader></uui-loader></div>`;
        }

        switch (this.activeTab) {
            case 'timeline': return this.renderTimelineTable();
            case 'audit':    return this.renderAuditTable();
            case 'log':      return this.renderLogTable();
            default:         return nothing;
        }
    }

    renderPagination() {
        if (this.totalRecords <= 0) return nothing;

        return html`
            <div class="pagination">
                <span class="record-info">${this.totalRecords} records</span>
                <div class="page-controls">
                    <uui-button look="outline" ?disabled=${this.isFirstPage}
                        @click=${() => this.goToPreviousPage()}>Prev</uui-button>
                    <span>Page ${this.currentPage} / ${this.totalPages}</span>
                    <uui-button look="outline" ?disabled=${this.isLastPage}
                        @click=${() => this.goToNextPage()}>Next</uui-button>
                </div>
            </div>`;
    }

    // ── Table Renderers ──────────────────────────────────

    renderEmptyState() {
        return html`<div class="empty">No records</div>`;
    }

    renderTimelineTable() {
        if (!this.items.length) return this.renderEmptyState();
        return html`
            <uui-table aria-label="Timeline">
                <uui-table-head>
                    <uui-table-head-cell>Date</uui-table-head-cell>
                    <uui-table-head-cell>User</uui-table-head-cell>
                    <uui-table-head-cell style="width: 50px">Source</uui-table-head-cell>
                    <uui-table-head-cell style="width: 250px">Action</uui-table-head-cell>
                    <uui-table-head-cell>Details</uui-table-head-cell>
                </uui-table-head>
                ${this.items.map((item) => html`
                    <uui-table-row>
                        <uui-table-cell>${this.formatDate(item.date)}</uui-table-cell>
                        <uui-table-cell>${item.user}</uui-table-cell>
                        <uui-table-cell style="width: 50px">
                            <uui-tag look=${item.source === 'audit' ? 'secondary' : 'primary'}>${item.source}</uui-tag>
                        </uui-table-cell>
                        <uui-table-cell style="width: 250px">${item.action}</uui-table-cell>
                        <uui-table-cell class="truncate">${this.formatTimelineDetails(item)}</uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>`;
    }

    renderAuditTable() {
        if (!this.items.length) return this.renderEmptyState();
        return html`
            <uui-table aria-label="Audit Trail">
                <uui-table-head>
                    <uui-table-head-cell>Date</uui-table-head-cell>
                    <uui-table-head-cell>User</uui-table-head-cell>
                    <uui-table-head-cell>Event Type</uui-table-head-cell>
                    <uui-table-head-cell>Details</uui-table-head-cell>
                    <uui-table-head-cell>IP</uui-table-head-cell>
                    <uui-table-head-cell>Affected</uui-table-head-cell>
                </uui-table-head>
                ${this.items.map((item) => html`
                    <uui-table-row>
                        <uui-table-cell>${this.formatDate(item.eventDateUtc)}</uui-table-cell>
                        <uui-table-cell>${item.performingDetails}</uui-table-cell>
                        <uui-table-cell><uui-tag look="primary">${item.eventType}</uui-tag></uui-table-cell>
                        <uui-table-cell class="truncate">${item.eventDetails}</uui-table-cell>
                        <uui-table-cell>${item.performingIp}</uui-table-cell>
                        <uui-table-cell>${item.affectedDetails}</uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>`;
    }

    renderLogTable() {
        if (!this.items.length) return this.renderEmptyState();
        return html`
            <uui-table aria-label="Content Logs">
                <uui-table-head>
                    <uui-table-head-cell>Date</uui-table-head-cell>
                    <uui-table-head-cell>User</uui-table-head-cell>
                    <uui-table-head-cell style="width: 100px">Log Type</uui-table-head-cell>
                    <uui-table-head-cell>Comment</uui-table-head-cell>
                    <uui-table-head-cell>Node ID</uui-table-head-cell>
                    <uui-table-head-cell>Node Name</uui-table-head-cell>
                    <uui-table-head-cell>Entity</uui-table-head-cell>
                </uui-table-head>
                ${this.items.map((item) => html`
                    <uui-table-row>
                        <uui-table-cell>${this.formatDate(item.dateStamp)}</uui-table-cell>
                        <uui-table-cell>${item.userName}</uui-table-cell>
                        <uui-table-cell style="width: 100px"><uui-tag look="primary">${item.logHeader}</uui-tag></uui-table-cell>
                        <uui-table-cell class="truncate">${item.logComment}</uui-table-cell>
                        <uui-table-cell>${item.nodeId}</uui-table-cell>
                        <uui-table-cell>${item.nodeName}</uui-table-cell>
                        <uui-table-cell>${item.entityType}</uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>`;
    }

    // ── Styles ───────────────────────────────────────────

    static styles = css`
        :host {
            display: block;
            padding: 20px;
        }

        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 16px;
        }

        .header h2 {
            margin: 0;
            font-size: 1.4rem;
        }

        .tabs {
            display: flex;
            gap: 8px;
            align-items: center;
        }

        .separator {
            width: 1px;
            height: 24px;
            background: var(--uui-color-border, #ccc);
            margin: 0 4px;
        }

        .filters {
            display: flex;
            gap: 10px;
            align-items: center;
            flex-wrap: wrap;
            margin-bottom: 16px;
            padding: 12px;
            background: var(--uui-color-surface-alt, #f4f4f4);
            border-radius: 6px;
        }

        .filter-spacer {
            flex: 1;
        }

        .select,
        .date-input {
            padding: 6px 10px;
            border: 1px solid var(--uui-color-border, #ccc);
            border-radius: 4px;
            font-size: 14px;
            background: var(--uui-color-surface, #fff);
            color: var(--uui-color-text, #333);
        }

        uui-input {
            min-width: 200px;
        }

        .center {
            display: flex;
            justify-content: center;
            padding: 40px;
        }

        .empty {
            text-align: center;
            padding: 40px;
            color: #888;
            font-style: italic;
        }

        .truncate {
            max-width: 300px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .pagination {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-top: 16px;
            padding-top: 12px;
            border-top: 1px solid #eee;
        }

        .page-controls {
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .record-info {
            color: #888;
            font-size: 0.9rem;
        }

        uui-table {
            width: 100%;
            table-layout: fixed;
        }

        uui-table-head-cell:nth-child(1),
        uui-table-cell:nth-child(1) {
            width: 170px;
            min-width: 170px;
            max-width: 170px;
        }

        uui-table-head-cell:nth-child(2),
        uui-table-cell:nth-child(2) {
            width: 220px;
            min-width: 220px;
            max-width: 220px;
        }
    `;
}

customElements.define('utpro-audit-log-dashboard', UtproAuditLogDashboard);
export default UtproAuditLogDashboard;
