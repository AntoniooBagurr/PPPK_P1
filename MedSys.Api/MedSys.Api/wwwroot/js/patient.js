document.addEventListener('DOMContentLoaded', () => {
    const id = qparam('id');
    if (!id) { toast('Nedostaje ?id=... u URL-u'); return; }
    qsel('#back').href = '/index.html';

    let visitsCache = [];
    let medsCache = [];

    async function load() {
        try {
            const d = await apiGet(`/api/patients/${id}`);
            const p = d.patient;

            // Header
            qsel('#title').textContent = `${p.firstName} ${p.lastName}`;
            qsel('#pInfo').textContent =
                `OIB: ${p.oib} • Rođen/a: ${fmtDate(p.birthDate)} • Spol: ${p.sex}` +
                (p.patientNumber ? ` • Broj pacijenta: ${p.patientNumber}` : '');

            // Povijest bolesti
            const mhRows = (d.medicalHistory ?? []).map(x => [
                x.diseaseName,
                fmtDate(x.startDate),
                x.endDate ? fmtDate(x.endDate) : ''
            ]);
            fillTable('#tblMh', mhRows);

            // Pregledi
            visitsCache = d.visits ?? [];
            const vRows = visitsCache.map(v => {
                const docsHtml = (v.documents?.length
                    ? v.documents.map(doc =>
                        `<a href="${doc.storageUrl}" target="_blank">${esc(doc.fileName)}</a> (${toSize(doc.sizeBytes)})`
                    ).join('<br>')
                    : '<i>nema</i>');

                const rxHtml = (v.prescriptions?.length
                    ? v.prescriptions.map(pr => {
                        const items = (pr.items ?? []).map(i =>
                            `${esc(i.medication)} — ${esc(i.dosage)} • ${esc(i.frequency)} • ${i.durationDays} dana`
                        ).join('<br>');
                        return `<div class="muted">${fmtDateTime(pr.issuedAt)}${pr.notes ? ' — ' + esc(pr.notes) : ''}<br>${items}</div>`;
                    }).join('<br>')
                    : '<i>nema</i>');

                return [
                    `${esc(v.visitType)} — ${fmtDateTime(v.visitDateTime)}`,
                    v.doctorName ?? '',
                    v.notes ?? '',
                    { __html: docsHtml },
                    { __html: rxHtml }
                ];
            });
            fillTable('#tblVisits', vRows);

            const optionsHtml = visitsCache
                .slice()
                .sort((a, b) => new Date(b.visitDateTime) - new Date(a.visitDateTime))
                .map(v => `<option value="${v.id}">${esc(v.visitType)} — ${fmtDateTime(v.visitDateTime)}</option>`)
                .join('');
            const selRx = qsel('#rx_visit');
            const selDoc = qsel('#doc_visit');
            if (selRx) selRx.innerHTML = optionsHtml;
            if (selDoc) selDoc.innerHTML = optionsHtml;

            const hasVisits = visitsCache.length > 0;
            const btnRx = qsel('#frmRx button[type="submit"]') || qsel('#frmRx button');
            const btnDoc = qsel('#frmDocVisit button[type="submit"]') || qsel('#frmDocVisit button');
            if (btnRx) btnRx.disabled = !hasVisits;
            if (btnDoc) btnDoc.disabled = !hasVisits;
            if (selRx) selRx.disabled = !hasVisits;
            if (selDoc) selDoc.disabled = !hasVisits;

            await loadPatientDocs();
        } catch (err) {
            toast(err?.message || 'Greška pri dohvaćanju podataka.');
        }
    }

    async function loadPatientDocs() {
        const docs = await apiGet(`/api/patients/${id}/documents`);
        const list = qsel('#patientDocs');
        if (!docs.length) { list.innerHTML = '<li><i>nema</i></li>'; return; }
        list.innerHTML = docs.map(d =>
            `<li><a href="${d.storageUrl}" target="_blank">${esc(d.fileName)}</a> — ${toSize(d.sizeBytes)} <small>(${fmtDateTime(d.uploadedAt)})</small></li>`
        ).join('');
    }

    // ======= Povijest bolesti – submit ================
    qsel('#frmMh').addEventListener('submit', async (e) => {
        e.preventDefault();
        try {
            const body = {
                patientId: id,
                diseaseName: qsel('#mhName').value.trim(),
                startDate: qsel('#mhStart').value,
                endDate: qsel('#mhEnd').value || null
            };
            await apiPostJson(`/api/patients/${id}/medicalhistory`, body);
            e.target.reset();
            await load();
        } catch (err) { toast(err?.message || 'Greška pri spremanju.'); }
    });

    // ======= Novi pregled – submit =============
    qsel('#frmVisitNew').addEventListener('submit', async (e) => {
        e.preventDefault();
        try {
            const whenVal = qsel('#vWhen').value;
            if (!whenVal) return toast('Odaberi datum/vrijeme.');
            const whenIso = new Date(whenVal).toISOString();
            const body = {
                patientId: id,
                visitDateTime: whenIso,
                visitType: qsel('#vType').value,
                notes: qsel('#vNotes').value || null,
                doctorId: null
            };
            await apiPostJson('/api/visits', body);
            e.target.reset();
            await load();
        } catch (err) { toast(err?.message || 'Greška pri dodavanju pregleda.'); }
    });


    let rxSelectedMed = null;  
    let rxDebTimer = null;

    const $rxMed = () => qsel('#rx_med');
    const $rxMedId = () => qsel('#rx_medId');
    const $rxSug = () => qsel('#rx_suggest');
    const $rxSugList = () => qsel('#rx_suggest ul');

    function openSuggest() { $rxSug().classList.add('open'); }
    function closeSuggest() { $rxSug().classList.remove('open'); }
    function clearSelection() {
        rxSelectedMed = null;
        $rxMedId().value = '';
    }
    function pickMed(m) {
        rxSelectedMed = m;
        $rxMed().value = m.name;
        $rxMedId().value = m.id;
        closeSuggest();
    }

    async function fetchMeds(q) {
        const url = q && q.length >= 1 ? `/api/medications?q=${encodeURIComponent(q)}` : `/api/medications`;
        try {
            medsCache = await apiGet(url);
        } catch {
            medsCache = [];
        }
        renderSuggest();
    }

    function renderSuggest() {
        const ul = $rxSugList();
        ul.innerHTML = medsCache.map((m, i) => `
          <li data-i="${i}">
            <span>${esc(m.name)}</span>
            ${m.atcCode ? `<span class="atc">${esc(m.atcCode)}</span>` : ''}
          </li>
        `).join('');
        openSuggest();

        ul.querySelectorAll('li').forEach(li => {
            li.addEventListener('click', () => {
                const i = Number(li.dataset.i);
                if (!isNaN(i) && medsCache[i]) pickMed(medsCache[i]);
            });
        });
    }


    $rxMed().addEventListener('input', (e) => {
        clearSelection();
        const q = e.target.value.trim();
        window.clearTimeout(rxDebTimer);
        rxDebTimer = window.setTimeout(() => fetchMeds(q), 180);
    });
    $rxMed().addEventListener('focus', () => {
        if (!medsCache.length) fetchMeds($rxMed().value.trim());
        else renderSuggest();
    });
    $rxMed().addEventListener('blur', () => setTimeout(() => closeSuggest(), 150));

  
    $rxMed().addEventListener('keydown', (e) => {
        const items = Array.from($rxSugList().querySelectorAll('li'));
        if (!items.length) return;

        const cur = items.findIndex(li => li.classList.contains('active'));
        const setActive = (idx) => {
            items.forEach(li => li.classList.remove('active'));
            if (idx >= 0 && idx < items.length) items[idx].classList.add('active');
        };

        if (e.key === 'ArrowDown') { e.preventDefault(); setActive(Math.min(cur + 1, items.length - 1)); openSuggest(); }
        if (e.key === 'ArrowUp') { e.preventDefault(); setActive(Math.max(cur - 1, 0)); openSuggest(); }
        if (e.key === 'Enter') {
            if (cur >= 0) { e.preventDefault(); items[cur].click(); }
        }
        if (e.key === 'Escape') { closeSuggest(); }
    });

    // ======= Recept – submit ===================
    qsel('#frmRx').addEventListener('submit', async (e) => {
        e.preventDefault();
        try {
            const visitId = qsel('#rx_visit').value;
            if (!visitId) return toast('Odaberi pregled.');

            const medId = $rxMedId().value;
            if (!medId) return toast('Odaberi lijek s popisa.');

            const body = {
                visitId,
                notes: qsel('#rx_notes').value || null,
                items: [{
                    medicationId: medId,            
                    medicationName: null,            
                    dosage: qsel('#rx_dosage').value.trim(),
                    frequency: qsel('#rx_freq').value.trim(),
                    durationDays: Number(qsel('#rx_days').value || 0)
                }]
            };
            await apiPostJson('/api/prescriptions', body);
            e.target.reset();
            clearSelection();
            await load();
        } catch (err) { toast(err?.message || 'Greška pri spremanju recepta.'); }
    });

    // ======= Upload dokumenta na PREGLED ===================================================
    qsel('#frmDocVisit').addEventListener('submit', async (e) => {
        e.preventDefault();
        try {
            const visitId = qsel('#doc_visit').value;
            const f = qsel('#doc_file').files[0];
            if (!visitId) return toast('Odaberi pregled.');
            if (!f) return toast('Odaberi datoteku.');
            const fd = new FormData(); fd.append('file', f);
            await apiPostForm(`/api/visits/${visitId}/documents`, fd);
            qsel('#doc_file').value = '';
            await load();
        } catch (err) {
            toast(err?.message || 'Greška pri uploadu dokumenta.');
        }
    });

    // ======= Upload dokumenta PACIJENTA ====================================================
    qsel('#btnUploadPatientDoc').addEventListener('click', async (e) => {
        e.preventDefault();
        const f = qsel('#pf_file').files[0];
        if (!f) return toast('Odaberi datoteku.');
        const fd = new FormData(); fd.append('file', f);
        await apiPostForm(`/api/patients/${id}/documents`, fd);
        qsel('#pf_file').value = '';
        await loadPatientDocs();
    });

    load();
});
