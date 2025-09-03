const apiBase = "";  

// ====== AUTH (JWT) ======
const TOKEN_KEY = "medsys.jwt";

function getToken() { return localStorage.getItem(TOKEN_KEY); }
function setToken(t) { localStorage.setItem(TOKEN_KEY, t); }
function clearToken() { localStorage.removeItem(TOKEN_KEY); }

function redirectToLogin() {
    const next = encodeURIComponent(location.pathname + location.search);
    location.href = `/login.html?next=${next}`;
}

async function apiFetch(path, opts = {}) {
    const token = getToken();
    if (!token) return redirectToLogin();

    const headers = new Headers(opts.headers || {});
    headers.set("Authorization", "Bearer " + token);
    if (!headers.has("Accept")) headers.set("Accept", "application/json");

    const res = await fetch(apiBase + path, { ...opts, headers });

    if (res.status === 401 || res.status === 403) {
        clearToken();
        return redirectToLogin();
    }

    if (!res.ok) {
        let msg = "";
        try { msg = await res.text(); } catch { }
        throw new Error(msg || res.statusText || "Greška pri pozivu API-ja.");
    }

    const ct = res.headers.get("content-type") || "";
    if (ct.includes("application/json")) return res.json();
    return res;
}

async function apiGet(path) {
    return apiFetch(path);
}

async function apiPostJson(path, body) {
    return apiFetch(path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
    });
}

async function apiPostForm(path, formData) {
    return apiFetch(path, { method: "POST", body: formData });
}

async function downloadFile(path, filename = "download") {
    const res = await apiFetch(path);
    const blob = await res.blob();
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    URL.revokeObjectURL(a.href);
    a.remove();
}


async function authBadge() {
    try {
        const me = await apiGet("/api/auth/me");
        const el = document.getElementById("whoami");
        if (el) el.textContent = me?.name || me?.email || "Prijavljen";
    } catch {  }
}

// ====== DOM & UI helpers ======
function qsel(sel) { return document.querySelector(sel); }
function qparam(name) { return new URL(location.href).searchParams.get(name); }
function esc(v) {
    return String(v ?? "").replace(/[&<>"']/g, c => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
}
function toast(msg) { alert(typeof msg === "string" ? msg : JSON.stringify(msg)); }
function fmtDate(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    return isNaN(d) ? "" : d.toLocaleDateString("hr-HR");
}
function fmtDateTime(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    return isNaN(d) ? "" : d.toLocaleString("hr-HR");
}
function toSize(bytes) {
    if (bytes == null) return "";
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
function fillTable(selector, rows) {
    const table = qsel(selector);
    const tbody = table?.querySelector("tbody");
    if (!tbody) return;
    tbody.innerHTML = rows.map(r => {
        const tds = r.map(cell =>
            typeof cell === "object" && cell?.__html !== undefined
                ? `<td>${cell.__html}</td>`
                : `<td>${esc(cell ?? "")}</td>`
        ).join("");
        return `<tr>${tds}</tr>`;
    }).join("");
}
