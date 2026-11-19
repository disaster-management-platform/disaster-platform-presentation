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