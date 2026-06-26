// Static configuration for the Audit Log Viewer dashboard.

export const API_BASE = '/umbraco/management/api/v1/utpro/audit-log';

export const TAB_ENDPOINTS = {
    timeline: 'timeline',
    audit: 'audit-entries',
    log: 'log-entries'
};

export const EXPORT_ENDPOINTS = {
    timeline: 'export/timeline',
    audit: 'export/audit-entries',
    log: 'export/log-entries'
};

export const SEARCH_PLACEHOLDERS = {
    timeline: 'Search user, action, details...',
    audit: 'Search details, user, IP, event type...',
    log: 'Search comment, user, entity, node ID...'
};

export const DEFAULT_PAGE_SIZE = 20;
