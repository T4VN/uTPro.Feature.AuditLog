// Pure formatting / value helpers (no DOM, no Lit templates).

/** Format a date value either in the browser's local time or UTC. */
export function formatDate(value, useUtcTime) {
    if (!value) return '';
    const date = new Date(value);
    return useUtcTime
        ? date.toLocaleString(undefined, { timeZone: 'UTC' }) + ' (UTC)'
        : date.toLocaleString();
}

/** Label for the local-time button, showing the real browser offset, e.g. "GMT+7". */
export function localTimeLabel() {
    const offsetMinutes = -new Date().getTimezoneOffset();
    const sign = offsetMinutes >= 0 ? '+' : '-';
    const abs = Math.abs(offsetMinutes);
    const hours = Math.floor(abs / 60);
    const minutes = abs % 60;
    return minutes === 0
        ? `GMT${sign}${hours}`
        : `GMT${sign}${hours}:${String(minutes).padStart(2, '0')}`;
}

/** Backoffice deep-link to edit a Document or Media node, or null when not linkable. */
export function nodeEditHref(entityType, nodeKey) {
    if (!nodeKey) return null;
    const type = (entityType || '').toLowerCase();
    if (type === 'document') return `/umbraco/section/content/workspace/document/edit/${nodeKey}`;
    if (type === 'media') return `/umbraco/section/media/workspace/media/edit/${nodeKey}`;
    return null;
}

const toIsoDate = (d) => {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
};

/** Resolve a quick-range key to { from, to } as yyyy-mm-dd strings. */
export function quickRange(range) {
    const today = new Date();
    let from = new Date(today);
    if (range === '7d') from.setDate(today.getDate() - 6);
    else if (range === '30d') from.setDate(today.getDate() - 29);
    else if (range === 'month') from = new Date(today.getFullYear(), today.getMonth(), 1);
    return { from: toIsoDate(from), to: toIsoDate(today) };
}
