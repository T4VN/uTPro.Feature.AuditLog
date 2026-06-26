// Workspace footer app: renders Export CSV + Local/UTC toggle in the Umbraco
// workspace footer bar, driven by the shared Audit Log context.

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css } from '@umbraco-cms/backoffice/external/lit';
import { localTimeLabel } from './format.js';
import { UTPRO_AUDIT_LOG_CONTEXT } from './context.js';

export class UtproAuditLogFooter extends UmbLitElement {

    static properties = {
        _useUtc: { state: true },
    };

    #context;

    constructor() {
        super();
        this._useUtc = false;
        this.consumeContext(UTPRO_AUDIT_LOG_CONTEXT, (ctx) => {
            this.#context = ctx;
            if (ctx) {
                this.observe(ctx.useUtc, (value) => { this._useUtc = value; });
            }
        });
    }

    render() {
        return html`
            <div class="footer">
                <div class="time-control">
                    <uui-button look=${!this._useUtc ? 'primary' : 'outline'} compact
                        @click=${() => this.#context?.setUseUtc(false)}>${localTimeLabel()}</uui-button>
                    <uui-button look=${this._useUtc ? 'primary' : 'outline'} compact
                        @click=${() => this.#context?.setUseUtc(true)}>UTC+0</uui-button>
                </div>
                <uui-button look="outline" @click=${() => this.#context?.exportCsv()}>Export CSV</uui-button>
            </div>`;
    }

    static styles = css`
        :host {
            display: flex;
            flex: 1;
        }
        .footer {
            display: flex;
            flex: 1;
            align-items: center;
            justify-content: space-between;
            box-sizing: border-box;
            padding-left: var(--uui-size-layout-1);
            padding-right: 0;
            gap: var(--uui-size-space-3);
        }
        .time-control {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
        }
    `;
}

customElements.define('utpro-audit-log-footer', UtproAuditLogFooter);
export default UtproAuditLogFooter;
