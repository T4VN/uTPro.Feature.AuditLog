// Presentational render functions. Each takes the dashboard element (`host`)
// and reads its state / calls its action methods.

import { html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { formatDate, nodeEditHref } from './format.js';

// ── Filters ──────────────────────────────────────────────

export function renderFilters(host) {
    return html`
        <div class="filters">
            <uui-input
                placeholder=${host.searchPlaceholder}
                .value=${host.filterSearch}
                @input=${(e) => { host.filterSearch = e.target.value; }}
                @keydown=${(e) => { if (e.key === 'Enter') host.applyFilters(); }}>
            </uui-input>

            <select style="width:150px" class="select" @change=${(e) => { host.filterUserId = e.target.value; }}>
                <option value="">All Users</option>
                ${host.users.map((user) => html`
                    <option value=${user.id} ?selected=${host.filterUserId == user.id}>
                        ${user.name} &lt;${user.email}&gt;
                    </option>`)}
            </select>

            ${host.activeTab === 'audit' ? html`
                <select style="width:150px" class="select" @change=${(e) => { host.filterAffectedUserId = e.target.value; }}>
                    <option value="">All Affected</option>
                    ${host.users.map((user) => html`
                        <option value=${user.id} ?selected=${host.filterAffectedUserId == user.id}>
                            ${user.name} &lt;${user.email}&gt;
                        </option>`)}
                </select>` : nothing}

            ${host.activeTab !== 'timeline' ? html`
                <select style="width:150px" class="select" @change=${(e) => { host.filterEventType = e.target.value; }}>
                    <option value="">All Types</option>
                    ${host.typeFilterOptions.map((type) => html`
                        <option value=${type} ?selected=${host.filterEventType === type}>${type}</option>`)}
                </select>` : nothing}

            <input type="date" class="date-input"
                .value=${host.filterDateFrom}
                @change=${(e) => host.setDateFrom(e.target.value)} />
            <input type="date" class="date-input"
                .value=${host.filterDateTo}
                @change=${(e) => host.setDateTo(e.target.value)} />

            <select class="select quick-select"
                .value=${host.rangeMode}
                @change=${(e) => host.setQuickRange(e.target.value)}>
                <option value="custom">Custom...</option>
                <option value="month">This month</option>
                <option value="30d">Last 30 days</option>
                <option value="7d">Last 7 days</option>
                <option value="today">Today</option>
            </select>

            <uui-button look="primary" @click=${() => host.applyFilters()}>Apply</uui-button>
            <uui-button look="secondary" @click=${() => host.resetFilters()}>Reset</uui-button>
        </div>`;
}

// ── Content switch ───────────────────────────────────────

export function renderContent(host) {
    if (host.isLoading) {
        return html`<div class="center"><uui-loader></uui-loader></div>`;
    }
    switch (host.activeTab) {
        case 'timeline': return renderTimelineTable(host);
        case 'audit':    return renderAuditTable(host);
        case 'log':      return renderLogTable(host);
        default:         return nothing;
    }
}

export function renderPagination(host, position = 'bottom') {
    return html`
        <div class="pagination ${position}">
            <span class="record-info">Total: ${host.totalRecords} records</span>
            <div class="page-controls">
                <uui-button look="outline" ?disabled=${host.isFirstPage}
                    @click=${() => host.goToPreviousPage()}>Prev</uui-button>
                <span class="page-jump">
                    Page
                    <input type="number" class="page-input" min="1" max=${host.totalPages}
                        .value=${String(host.currentPage)}
                        @change=${(e) => host.goToPage(e.target.value)}
                        @keydown=${(e) => { if (e.key === 'Enter') host.goToPage(e.target.value); }} />
                    / ${host.totalPages}
                </span>
                <uui-button look="outline" ?disabled=${host.isLastPage}
                    @click=${() => host.goToNextPage()}>Next</uui-button>
            </div>
        </div>`;
}

// ── Shared cell helpers ──────────────────────────────────

function sortableHeader(host, label, column) {
    return html`
        <uui-table-head-cell
            style="cursor:pointer;user-select:none;"
            title="Sort by ${label}"
            @click=${() => host.toggleSort(column)}>
            ${label}
            <uui-symbol-sort
                ?active=${host.sortColumn === column}
                ?descending=${host.sortDirection === 'desc'}></uui-symbol-sort>
        </uui-table-head-cell>`;
}

function nodeNameCell(item) {
    if (!item.nodeName) return '';
    const href = nodeEditHref(item.entityType, item.nodeKey);
    return href ? html`<a href=${href}>${item.nodeName}</a>` : html`${item.nodeName}`;
}

function timelineDetails(item) {
    const parts = [];
    if (item.source === 'log') {
        if (item.nodeId && item.nodeId > 0) {
            const label = item.nodeName ? `#${item.nodeId} - ${item.nodeName}` : `#${item.nodeId}`;
            const href = nodeEditHref(item.extra, item.nodeKey);
            parts.push(href ? html`<a href=${href}>${label}</a>` : html`${label}`);
        }
        if (item.details) parts.push(html`${item.details}`);
    } else {
        if (item.details) parts.push(html`${item.details}`);
        if (item.extra) parts.push(html`${item.extra}`);
    }
    if (parts.length === 0) return '';
    if (parts.length === 1) return parts[0];
    return html`${parts[0]}<br />${parts[1]}`;
}

function emptyState() {
    return html`<div class="empty">No records</div>`;
}

// ── Tables ───────────────────────────────────────────────

export function renderTimelineTable(host) {
    if (!host.items.length) return emptyState();
    return html`
        <uui-table aria-label="Timeline">
            <uui-table-head>
                ${sortableHeader(host, 'Date', 'date')}
                ${sortableHeader(host, 'User', 'user')}
                ${sortableHeader(host, 'Source', 'source')}
                ${sortableHeader(host, 'Action', 'action')}
                <uui-table-head-cell>Details</uui-table-head-cell>
            </uui-table-head>
            ${host.items.map((item) => html`
                <uui-table-row>
                    <uui-table-cell>${formatDate(item.date, host.useUtcTime)}</uui-table-cell>
                    <uui-table-cell>${item.user}</uui-table-cell>
                    <uui-table-cell>
                        <uui-tag look=${item.source === 'audit' ? 'secondary' : 'primary'}>${item.source}</uui-tag>
                    </uui-table-cell>
                    <uui-table-cell>${item.action}</uui-table-cell>
                    <uui-table-cell class="truncate">${timelineDetails(item)}</uui-table-cell>
                </uui-table-row>`)}
        </uui-table>`;
}

export function renderAuditTable(host) {
    if (!host.items.length) return emptyState();
    return html`
        <uui-table aria-label="Audit Trail">
            <uui-table-head>
                ${sortableHeader(host, 'Date', 'date')}
                ${sortableHeader(host, 'User', 'user')}
                ${sortableHeader(host, 'Event Type', 'eventType')}
                <uui-table-head-cell>Details</uui-table-head-cell>
                ${sortableHeader(host, 'IP', 'ip')}
                ${sortableHeader(host, 'Affected', 'affected')}
            </uui-table-head>
            ${host.items.map((item) => html`
                <uui-table-row>
                    <uui-table-cell>${formatDate(item.eventDateUtc, host.useUtcTime)}</uui-table-cell>
                    <uui-table-cell>${item.performingDetails}</uui-table-cell>
                    <uui-table-cell><uui-tag look="primary">${item.eventType}</uui-tag></uui-table-cell>
                    <uui-table-cell class="truncate">${item.eventDetails}</uui-table-cell>
                    <uui-table-cell>${item.performingIp}</uui-table-cell>
                    <uui-table-cell>${item.affectedDetails}</uui-table-cell>
                </uui-table-row>`)}
        </uui-table>`;
}

export function renderLogTable(host) {
    if (!host.items.length) return emptyState();
    return html`
        <uui-table aria-label="Content Logs">
            <uui-table-head>
                ${sortableHeader(host, 'Date', 'date')}
                ${sortableHeader(host, 'User', 'user')}
                ${sortableHeader(host, 'Log Type', 'logHeader')}
                <uui-table-head-cell>Comment</uui-table-head-cell>
                ${sortableHeader(host, 'Node ID', 'nodeId')}
                ${sortableHeader(host, 'Node Name', 'nodeName')}
                ${sortableHeader(host, 'Entity', 'entity')}
            </uui-table-head>
            ${host.items.map((item) => html`
                <uui-table-row>
                    <uui-table-cell>${formatDate(item.dateStamp, host.useUtcTime)}</uui-table-cell>
                    <uui-table-cell>${item.userName}</uui-table-cell>
                    <uui-table-cell><uui-tag look="primary">${item.logHeader}</uui-tag></uui-table-cell>
                    <uui-table-cell class="truncate">${item.logComment}</uui-table-cell>
                    <uui-table-cell>${item.nodeId}</uui-table-cell>
                    <uui-table-cell class="wrap-anywhere">${nodeNameCell(item)}</uui-table-cell>
                    <uui-table-cell>${item.entityType}</uui-table-cell>
                </uui-table-row>`)}
        </uui-table>`;
}
