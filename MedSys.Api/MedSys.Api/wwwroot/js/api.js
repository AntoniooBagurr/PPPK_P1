const apiBase = ""; 

// --- HTTP helpers ---
async function apiGet(path) {
    const r = await fetch(apiBase + path);
    if (!r.ok) throw new Error(await r.text());
    return r.json();
}
async function apiPostJson(path, body) {
    const r = await fetch(apiBase + path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
    });
    if (!r.ok) throw new Error(await r.text());
    try { return await r.json(); } catch { return {}; }
}
async function apiPostForm(path, formData) {
    const r = await fetch(apiBase + path, { method: "POST", body: formData });
    if (!r.ok) throw new Error(await r.text());
    try { return await r.json(); } catch { return {}; }
}

// --- DOM & UI helpers ---
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
