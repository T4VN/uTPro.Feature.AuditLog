// Thin wrapper around the Management API, handling backoffice auth headers.

import { API_BASE } from './config.js';

async function authRequestInit(authContext, body) {
    const config = authContext?.getOpenApiConfiguration();
    const headers = { 'Content-Type': 'application/json' };

    if (config?.token) {
        const token = await config.token();
        if (token) headers['Authorization'] = `Bearer ${token}`;
    }

    return {
        method: 'POST',
        headers,
        credentials: config?.credentials || 'same-origin',
        body: JSON.stringify(body ?? {})
    };
}

/** POST to an audit-log endpoint and return the parsed JSON body. */
export async function fetchJson(authContext, endpoint, body = {}) {
    const init = await authRequestInit(authContext, body);
    const response = await fetch(`${API_BASE}/${endpoint}`, init);
    if (!response.ok) throw new Error(`API error: ${response.status}`);
    return response.json();
}

/** POST to an audit-log endpoint and return the raw Blob (used for CSV export). */
export async function fetchBlob(authContext, endpoint, body = {}) {
    const init = await authRequestInit(authContext, body);
    const response = await fetch(`${API_BASE}/${endpoint}`, init);
    if (!response.ok) throw new Error('Export failed');
    return response.blob();
}

/** Trigger a browser download for a Blob. */
export function downloadBlob(blob, fileName) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
}
