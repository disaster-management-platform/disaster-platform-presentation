// Mapa Leaflet (dashboards y detalle de desastre)
const mapEl = document.getElementById('map');
if (mapEl && window.L) {
    const pts = JSON.parse(mapEl.dataset.points || '[]');
    const map = L.map(mapEl).setView([4.6, -75.6], 7);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 18, attribution: '&copy; OpenStreetMap'
    }).addTo(map);
    const color = s => (s >= 4 ? '#dc3545' : s === 3 ? '#fd7e14' : '#555555');
    const bounds = [];
    pts.forEach(p => {
        L.circleMarker([p.lat, p.lng], { radius: 6 + p.sev * 2, color: color(p.sev), fillOpacity: 0.5 })
            .addTo(map).bindPopup(`<b>${p.name}</b><br>Severidad ${p.sev}`);
        bounds.push([p.lat, p.lng]);
    });
    if (bounds.length > 1) map.fitBounds(bounds, { padding: [30, 30] });
    else if (bounds.length === 1) map.setView(bounds[0], 10);
}

// Filas clicables
document.querySelectorAll('tr[data-href]').forEach(tr =>
    tr.addEventListener('click', () => { location.href = tr.dataset.href; }));

document.querySelectorAll('table[data-selectable="true"]').forEach(table => {
    const rowChecks = [...table.querySelectorAll('[data-select-row]')];
    const selectAll = table.querySelector('[data-select-all]');
    const modal = document.querySelector('#dlg-proyectos');
    const idsInput = modal?.querySelector('[data-selected-ids]');
    const countLabel = modal?.querySelector('[data-selected-count]');
    const launchButton = document.querySelector('[data-selection-launch="true"]');
    const submitButton = modal?.querySelector('[data-selection-submit]');

    const updateSelection = () => {
        const selectable = rowChecks.filter(check => !check.disabled);
        const selected = rowChecks.filter(check => check.checked && !check.disabled);
        if (idsInput) idsInput.value = selected.map(check => check.value).filter(Boolean).join(',');
        if (countLabel) countLabel.textContent = String(selected.length);
        if (launchButton) launchButton.disabled = selected.length === 0;
        if (submitButton) submitButton.disabled = selected.length === 0;
        if (selectAll) {
            selectAll.disabled = selectable.length === 0;
            selectAll.checked = selectable.length > 0 && selected.length === selectable.length;
            selectAll.indeterminate = selected.length > 0 && selected.length < selectable.length;
        }
    };

    rowChecks.forEach(check => {
        check.addEventListener('click', event => event.stopPropagation());
        check.addEventListener('change', updateSelection);
    });
    selectAll?.addEventListener('change', () => {
        rowChecks.forEach(check => { if (!check.disabled) check.checked = selectAll.checked; });
        updateSelection();
    });
    updateSelection();
});

// Búsqueda y filtros de las tablas (en el navegador, sobre los datos ya renderizados)
document.querySelectorAll('.table-card').forEach(card => {
    const rows = card.querySelectorAll('tbody tr');
    const search = card.querySelector('[data-table-search]');
    const filters = card.querySelectorAll('[data-table-filter]');
    const apply = () => {
        const q = (search?.value || '').toLowerCase();
        rows.forEach(tr => {
            let ok = !q || tr.textContent.toLowerCase().includes(q);
            filters.forEach(f => {
                if (!f.value) return;
                const td = tr.children[Number(f.dataset.tableFilter)];
                ok = ok && !!td && td.textContent.toLowerCase().includes(f.value.toLowerCase());
            });
            tr.hidden = !ok;
        });
    };
    search?.addEventListener('input', apply);
    filters.forEach(f => f.addEventListener('change', apply));
});