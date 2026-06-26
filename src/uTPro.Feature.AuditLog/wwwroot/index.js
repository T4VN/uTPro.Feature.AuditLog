import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

import { TAB_ENDPOINTS, EXPORT_ENDPOINTS, SEARCH_PLACEHOLDERS, DEFAULT_PAGE_SIZE } from './config.js';
import { fetchJson, fetchBlob, downloadBlob } from './api.js';
import { localTimeLabel, quickRange } from './format.js';
import { dashboardStyles } from './styles.js';
import { renderFilters, renderContent, renderPagination } from './render.js';
import { UTPRO_AUDIT_LOG_CONTEXT } from './context.js';

/**
 * Base dashboard element. Holds all state + actions and wires the presentational
 * render functions (render.js) and helpers (api.js / format.js) together.
 * Each tab is a thin subclass (see view-*.js) that only sets `activeTab`.
 */
export class UtproAuditLogDashboard extends UmbLitElement {

    static properties = {
        activeTab:            { state: true },
        isLoading:            { state: true },
        items:                { state: true },
        totalRecords:         { state: true },
        currentSkip:          { state: true },
        pageSize:             { state: true },
        eventTypes:           { state: true },
        logHeaders:           { state: true },
        users:                { state: true },
        filterEventType:      { state: true },
        filterSearch:         { state: true },
        filterDateFrom:       { state: true },
        filterDateTo:         { state: true },
        filterUserId:         { state: true },
        filterAffectedUserId: { state: true },
        useUtcTime:           { state: true },
        sortColumn:           { state: true },
        sortDirection:        { state: true },
        rangeMode:            { state: true },
    };

    static styles = dashboardStyles;

    #authContext;
    #auditContext;

    constructor() {
        super();
        this.activeTab = 'timeline';
        this.isLoading = false;
        this.items = [];
        this.totalRecords = 0;
        this.currentSkip = 0;
        this.pageSize = DEFAULT_PAGE_SIZE;
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
        this.sortColumn = 'date';
        this.sortDirection = 'desc';
        // Default range: This month (also pre-fills the two date inputs).
        const defaultRange = quickRange('month');
        this.filterDateFrom = defaultRange.from;
        this.filterDateTo = defaultRange.to;
        this.rangeMode = 'month';
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => {
            this.#authContext = ctx;
        });
        this.consumeContext(UTPRO_AUDIT_LOG_CONTEXT, (ctx) => {
            this.#auditContext = ctx;
            if (ctx) {
                ctx.setActiveView(this);
                this.observe(ctx.useUtc, (value) => { this.useUtcTime = value; });
            }
        });
    }

    async connectedCallback() {
        super.connectedCallback();
        this.readUrlParams();
        await this.loadFilterOptions();
        await this.loadData();
    }

    // ── URL state (shareable / survives refresh) ─────────

    readUrlParams() {
        const p = new URLSearchParams(window.location.search);
        if ([...p.keys()].length === 0) return; // no params → keep defaults (This month)
        this.filterSearch = p.get('search') ?? '';
        this.filterUserId = p.get('userId') ?? '';
        this.filterAffectedUserId = p.get('affectedUserId') ?? '';
        this.filterEventType = p.get('eventType') ?? '';
        this.filterDateFrom = p.get('dateFrom') ?? '';
        this.filterDateTo = p.get('dateTo') ?? '';
        this.rangeMode = p.get('range') ?? 'custom';
        this.sortColumn = p.get('sort') ?? 'date';
        this.sortDirection = p.get('dir') ?? 'desc';
        const page = parseInt(p.get('page') ?? '1', 10);
        this.currentSkip = (!isNaN(page) && page > 1) ? (page - 1) * this.pageSize : 0;
    }

    syncUrl() {
        const p = new URLSearchParams();
        if (this.filterSearch) p.set('search', this.filterSearch);
        if (this.filterUserId) p.set('userId', this.filterUserId);
        if (this.filterAffectedUserId) p.set('affectedUserId', this.filterAffectedUserId);
        if (this.filterEventType) p.set('eventType', this.filterEventType);
        if (this.filterDateFrom) p.set('dateFrom', this.filterDateFrom);
        if (this.filterDateTo) p.set('dateTo', this.filterDateTo);
        if (this.rangeMode) p.set('range', this.rangeMode);
        if (this.sortColumn && this.sortColumn !== 'date') p.set('sort', this.sortColumn);
        if (this.sortDirection && this.sortDirection !== 'desc') p.set('dir', this.sortDirection);
        if (this.currentPage > 1) p.set('page', String(this.currentPage));

        const url = new URL(window.location.href);
        url.search = p.toString();
        window.history.replaceState(window.history.state, '', url);
    }

    clearUrl() {
        const url = new URL(window.location.href);
        url.search = '';
        window.history.replaceState(window.history.state, '', url);
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        this.#auditContext?.clearActiveView(this);
    }

    // ── Data ─────────────────────────────────────────────

    buildRequestBody(includePaging = true) {
        const body = {};
        if (includePaging) {
            body.skip = this.currentSkip;
            body.take = this.pageSize;
        }
        if (this.filterEventType) body.eventType = this.filterEventType;
        if (this.filterSearch) body.searchTerm = this.filterSearch;
        if (this.filterDateFrom) body.dateFrom = `${this.filterDateFrom}T00:00:00`;
        if (this.filterDateTo) body.dateTo = `${this.filterDateTo}T23:59:59`;
        if (this.filterUserId) body.userId = parseInt(this.filterUserId);
        if (this.filterAffectedUserId) body.affectedUserId = parseInt(this.filterAffectedUserId);
        if (this.sortColumn) {
            body.sortColumn = this.sortColumn;
            body.sortDirection = this.sortDirection;
        }
        return body;
    }

    async loadFilterOptions() {
        try {
            const [eventTypes, logHeaders, users] = await Promise.all([
                fetchJson(this.#authContext, 'event-types'),
                fetchJson(this.#authContext, 'log-headers'),
                fetchJson(this.#authContext, 'users')
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
            const data = await fetchJson(this.#authContext, TAB_ENDPOINTS[this.activeTab], this.buildRequestBody());
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

    applyFilters() {
        this.currentSkip = 0;
        this.loadData();
        this.syncUrl();
    }

    resetFilters() {
        this.filterEventType = '';
        this.filterSearch = '';
        this.filterUserId = '';
        this.filterAffectedUserId = '';
        this.sortColumn = 'date';
        this.sortDirection = 'desc';
        const { from, to } = quickRange('month');
        this.filterDateFrom = from;
        this.filterDateTo = to;
        this.rangeMode = 'month';
        this.currentSkip = 0;
        this.clearUrl();
        this.loadData();
    }

    toggleSort(column) {
        if (this.sortColumn === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortColumn = column;
            this.sortDirection = column === 'date' ? 'desc' : 'asc';
        }
        this.currentSkip = 0;
        this.loadData();
        this.syncUrl();
    }

    setQuickRange(range) {
        this.rangeMode = range;
        if (range === 'custom') return; // keep the dates the user typed
        const { from, to } = quickRange(range);
        this.filterDateFrom = from;
        this.filterDateTo = to;
        this.applyFilters();
    }

    setDateFrom(value) {
        this.filterDateFrom = value;
        this.rangeMode = 'custom';
    }

    setDateTo(value) {
        this.filterDateTo = value;
        this.rangeMode = 'custom';
    }

    async exportCsv() {
        try {
            const blob = await fetchBlob(this.#authContext, EXPORT_ENDPOINTS[this.activeTab], this.buildRequestBody(false));
            downloadBlob(blob, `${this.activeTab}-${new Date().toISOString().slice(0, 10)}.csv`);
        } catch (error) {
            console.error('Export failed:', error);
        }
    }

    goToPreviousPage() {
        if (this.currentSkip > 0) {
            this.currentSkip = Math.max(0, this.currentSkip - this.pageSize);
            this.loadData();
            this.syncUrl();
        }
    }

    goToNextPage() {
        if (this.currentSkip + this.pageSize < this.totalRecords) {
            this.currentSkip += this.pageSize;
            this.loadData();
            this.syncUrl();
        }
    }

    goToPage(value) {
        let page = parseInt(value, 10);
        if (isNaN(page)) page = this.currentPage;
        page = Math.min(Math.max(1, page), this.totalPages);
        this.currentSkip = (page - 1) * this.pageSize;
        this.loadData();
        this.syncUrl();
    }

    // ── Computed (used by render functions) ──────────────

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

    get localTimeLabel() {
        return localTimeLabel();
    }

    // ── Render ───────────────────────────────────────────

    render() {
        return html`
            ${renderFilters(this)}
            ${renderContent(this)}
            ${renderPagination(this, 'bottom')}`;
    }
}

customElements.define('utpro-audit-log-dashboard', UtproAuditLogDashboard);
export default UtproAuditLogDashboard;
