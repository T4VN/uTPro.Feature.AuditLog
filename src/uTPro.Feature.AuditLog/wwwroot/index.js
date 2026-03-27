import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
const API = '/umbraco/management/api/v1/utpro/audit-log';
export class UtproAuditLogDashboard extends UmbLitElement {
    static properties = {
        _activeTab: { state: true }, _loading: { state: true }, _items: { state: true },
        _total: { state: true }, _skip: { state: true }, _take: { state: true },
        _eventTypes: { state: true }, _logHeaders: { state: true },
        _filterEventType: { state: true }, _filterSearch: { state: true },
        _filterDateFrom: { state: true }, _filterDateTo: { state: true },
    };
    #auth;
    constructor() {
        super(); this._activeTab = 'audit'; this._loading = false; this._items = [];
        this._total = 0; this._skip = 0; this._take = 20; this._eventTypes = []; this._logHeaders = [];
        this._filterEventType = ''; this._filterSearch = ''; this._filterDateFrom = ''; this._filterDateTo = '';
        this.consumeContext(UMB_AUTH_CONTEXT, c => { this.#auth = c; });
    }
    async connectedCallback() { super.connectedCallback(); await this._loadFilters(); await this._load(); }
    async _api(url, body = {}) {
        const cfg = this.#auth?.getOpenApiConfiguration(); const h = {};
        if (cfg?.token) { const t = await cfg.token(); if (t) h['Authorization'] = 'Bearer ' + t; }
        const r = await fetch(url, {
            method: 'POST', headers: { ...h, 'Content-Type': 'application/json' },
            credentials: cfg?.credentials || 'same-origin', body: JSON.stringify(body)
        });
        if (!r.ok) throw new Error('API error'); return r.json();
    }
    async _loadFilters() {
        try {
            const [t, h] = await Promise.all([this._api(API + '/event-types'), this._api(API + '/log-headers')]);
            this._eventTypes = t || []; this._logHeaders = h || [];
        } catch (e) { console.error(e); }
    }
    async _load() {
        this._loading = true;
        try {
            const ep = this._activeTab === 'audit' ? 'audit-entries' : 'log-entries';
            const b = { skip: this._skip, take: this._take };
            if (this._filterEventType) b.eventType = this._filterEventType;
            if (this._filterSearch) b.searchTerm = this._filterSearch;
            if (this._filterDateFrom) b.dateFrom = this._filterDateFrom;
            if (this._filterDateTo) b.dateTo = this._filterDateTo;
            const d = await this._api(API + '/' + ep, b); this._items = d.items || []; this._total = d.total || 0;
        } catch (e) { this._items = []; this._total = 0; } this._loading = false;
    }
    _switchTab(t) {
        this._activeTab = t; this._skip = 0; this._filterEventType = ''; this._filterSearch = '';
        this._filterDateFrom = ''; this._filterDateTo = ''; this._load();
    }
    _apply() { this._skip = 0; this._load(); }
    _reset() { this._filterEventType = ''; this._filterSearch = ''; this._filterDateFrom = ''; this._filterDateTo = ''; this._skip = 0; this._load(); }
    _prev() { if (this._skip > 0) { this._skip = Math.max(0, this._skip - this._take); this._load(); } }
    _next() { if (this._skip + this._take < this._total) { this._skip += this._take; this._load(); } }
    get _page() { return Math.floor(this._skip / this._take) + 1; }
    get _pages() { return Math.max(1, Math.ceil(this._total / this._take)); }
    _d(v) { return v ? new Date(v).toLocaleString() : ''; }
    render() {
        const opts = this._activeTab === 'audit' ? this._eventTypes : this._logHeaders;
        return html`<uui-box>
            <div class="hdr"><h2>Audit Log</h2>
                <div class="tabs">
                    <uui-button look=${this._activeTab === 'audit' ? 'primary' : 'secondary'} @click=${() => this._switchTab('audit')}>Audit Trail</uui-button>
                    <uui-button look=${this._activeTab === 'log' ? 'primary' : 'secondary'} @click=${() => this._switchTab('log')}>Content Logs</uui-button>
                </div></div>
            <div class="flt">
                <uui-input placeholder="Search..." .value=${this._filterSearch} @input=${e => { this._filterSearch = e.target.value; }}></uui-input>
                <select class="sel" @change=${e => { this._filterEventType = e.target.value; }}><option value="">All Types</option>
                    ${opts.map(t => html`<option value=${t} ?selected=${this._filterEventType === t}>${t}</option>`)}</select>
                <input type="date" class="dt" .value=${this._filterDateFrom} @change=${e => { this._filterDateFrom = e.target.value; }} />
                <input type="date" class="dt" .value=${this._filterDateTo} @change=${e => { this._filterDateTo = e.target.value; }} />
                <uui-button look="primary" @click=${() => this._apply()}>Apply</uui-button>
                <uui-button look="secondary" @click=${() => this._reset()}>Reset</uui-button>
            </div>
            ${this._loading ? html`<div class="ctr"><uui-loader></uui-loader></div>` : this._activeTab === 'audit' ? this._rA() : this._rL()}
            ${this._total > 0 ? html`<div class="pgr"><span class="inf">${this._total} records</span>
                <div class="pc"><uui-button look="outline" ?disabled=${this._skip === 0} @click=${() => this._prev()}>Prev</uui-button>
                <span>Page ${this._page} / ${this._pages}</span>
                <uui-button look="outline" ?disabled=${this._skip + this._take >= this._total} @click=${() => this._next()}>Next</uui-button></div></div>` : nothing}
        </uui-box>`;
    }
    _rA() {
        if (!this._items.length) return html`<div class="emp">No records</div>`;
        return html`<uui-table aria-label="Audit"><uui-table-head>
            <uui-table-head-cell>Date</uui-table-head-cell><uui-table-head-cell>User</uui-table-head-cell>
            <uui-table-head-cell>Event Type</uui-table-head-cell><uui-table-head-cell>Details</uui-table-head-cell>
            <uui-table-head-cell>IP</uui-table-head-cell><uui-table-head-cell>Affected</uui-table-head-cell>
        </uui-table-head>${this._items.map(i => html`<uui-table-row>
            <uui-table-cell>${this._d(i.eventDateUtc)}</uui-table-cell><uui-table-cell>${i.performingDetails}</uui-table-cell>
            <uui-table-cell><uui-tag look="primary">${i.eventType}</uui-tag></uui-table-cell>
            <uui-table-cell class="tr">${i.eventDetails}</uui-table-cell><uui-table-cell>${i.performingIp}</uui-table-cell>
            <uui-table-cell>${i.affectedDetails}</uui-table-cell></uui-table-row>`)}</uui-table>`;
    }
    _rL() {
        if (!this._items.length) return html`<div class="emp">No records</div>`;
        return html`<uui-table aria-label="Logs"><uui-table-head>
            <uui-table-head-cell>Date</uui-table-head-cell><uui-table-head-cell>User</uui-table-head-cell>
            <uui-table-head-cell>Log Type</uui-table-head-cell><uui-table-head-cell>Comment</uui-table-head-cell>
            <uui-table-head-cell>Node ID</uui-table-head-cell><uui-table-head-cell>Entity</uui-table-head-cell>
        </uui-table-head>${this._items.map(i => html`<uui-table-row>
            <uui-table-cell>${this._d(i.dateStamp)}</uui-table-cell><uui-table-cell>${i.userName}</uui-table-cell>
            <uui-table-cell><uui-tag look="primary">${i.logHeader}</uui-tag></uui-table-cell>
            <uui-table-cell class="tr">${i.logComment}</uui-table-cell><uui-table-cell>${i.nodeId}</uui-table-cell>
            <uui-table-cell>${i.entityType}</uui-table-cell></uui-table-row>`)}</uui-table>`;
    }
    static styles = css`
        :host{display:block;padding:20px} .hdr{display:flex;justify-content:space-between;align-items:center;margin-bottom:16px}
        .hdr h2{margin:0;font-size:1.4rem} .tabs{display:flex;gap:8px}
        .flt{display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:16px;padding:12px;background:var(--uui-color-surface-alt,#f4f4f4);border-radius:6px}
        .sel,.dt{padding:6px 10px;border:1px solid var(--uui-color-border,#ccc);border-radius:4px;font-size:14px;background:var(--uui-color-surface,#fff);color:var(--uui-color-text,#333)}
        uui-input{min-width:200px} .ctr{display:flex;justify-content:center;padding:40px}
        .emp{text-align:center;padding:40px;color:#888;font-style:italic}
        .tr{max-width:300px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
        .pgr{display:flex;justify-content:space-between;align-items:center;margin-top:16px;padding-top:12px;border-top:1px solid #eee}
        .pc{display:flex;align-items:center;gap:10px} .inf{color:#888;font-size:.9rem} uui-table{width:100%}`;
}
customElements.define('utpro-audit-log-dashboard', UtproAuditLogDashboard);
export default UtproAuditLogDashboard;
