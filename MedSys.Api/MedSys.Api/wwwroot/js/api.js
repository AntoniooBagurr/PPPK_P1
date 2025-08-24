const apiBase = ""; // isti origin (https://localhost:7290). Ako je drugi port, stavi npr. 'https://localhost:7290'

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
    return r.json();
}

function qsel(sel) { return document.querySelector(sel); }
function qparam(name) { return new URL(location.href).searchParams.get(name); }
