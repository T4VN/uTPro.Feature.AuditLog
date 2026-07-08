import { css } from '@umbraco-cms/backoffice/external/lit';

export const dashboardStyles = css`
    :host {
        display: block;
        padding: var(--uui-size-layout-1);
    }

    .separator {
        width: 1px;
        height: var(--uui-size-6);
        background: var(--uui-color-border);
        margin: 0 var(--uui-size-space-1);
    }

    .filters {
        display: flex;
        gap: var(--uui-size-space-4);
        align-items: center;
        flex-wrap: wrap;
        margin-bottom: var(--uui-size-space-5);
        padding: var(--uui-size-space-4) 0;
        background: var(--uui-color-surface-alt);
        border-radius: var(--uui-border-radius);
    }

    .filter-spacer {
        flex: 1;
    }

    .time-control {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-2);
    }

    .quick-select {
        min-width: 120px;
    }

    a {
        color: var(--uui-color-interactive);
        text-decoration: none;
        font-weight: 700;
    }

    a:hover {
        text-decoration: underline;
    }

    .select,
    .date-input {
        padding: var(--uui-size-space-2) var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        font-size: var(--uui-type-small-size, 14px);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
    }

    uui-input {
        min-width: 200px;
    }

    .center {
        display: flex;
        justify-content: center;
        padding: var(--uui-size-layout-1);
    }

    .empty {
        text-align: center;
        padding: var(--uui-size-layout-1);
        color: var(--uui-color-text-alt);
        font-style: italic;
    }

    .truncate {
        max-width: 300px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    /* Wrap long unbroken values (e.g. dotted dictionary keys) inside the fixed-width
       column instead of overflowing into the next column. */
    .wrap-anywhere {
        white-space: normal;
        overflow-wrap: anywhere;
        word-break: break-word;
    }

    .wrap-anywhere a {
        display: inline-block;
        max-width: 100%;
    }

    .pagination {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: var(--uui-size-space-4);
        flex-wrap: wrap;
    }

    .pagination.top {
        margin-bottom: var(--uui-size-space-4);
        padding-bottom: var(--uui-size-space-4);
        border-bottom: 1px solid var(--uui-color-divider);
    }

    .pagination.bottom {
        margin-top: var(--uui-size-space-4);
        padding-top: var(--uui-size-space-4);
        border-top: 1px solid var(--uui-color-divider);
    }

    .page-jump {
        display: inline-flex;
        align-items: center;
        gap: var(--uui-size-space-2);
    }

    .page-input {
        width: 56px;
        padding: var(--uui-size-space-1) var(--uui-size-space-2);
        text-align: center;
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        font-size: var(--uui-type-small-size, 14px);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
    }

    .record-info {
        color: var(--uui-color-text-alt);
        font-size: var(--uui-type-small-size, 14px);
    }

    uui-table {
        width: 100%;
        table-layout: fixed;
    }

    uui-table-head-cell:nth-child(1),
    uui-table-cell:nth-child(1) {
        width: 170px;
    }

    uui-table-head-cell:nth-child(2),
    uui-table-cell:nth-child(2) {
        width: 220px;
    }
`;
