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

const scheduleEditor = document.querySelector('[data-schedule-editor]');
if (scheduleEditor) {
    const body = scheduleEditor.querySelector('[data-schedule-rows]');
    const hidden = document.querySelector('[data-schedule-value]');
    const addButton = scheduleEditor.querySelector('[data-add-activity]');
    const budgetInput = document.querySelector('#f-finalBudget');
    const summary = scheduleEditor.querySelector('[data-budget-summary]');
    const currency = new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 });

    const inputFor = (type, key, label, value = '') => {
        const input = document.createElement('input');
        input.type = type;
        input.className = 'form-control';
        input.dataset.scheduleField = key;
        input.setAttribute('aria-label', label);
        input.value = value;
        if (type === 'number') input.step = 'any';
        return input;
    };

    const addActivity = (values = []) => {
        const row = document.createElement('tr');
        const fields = [
            ['text', 'task', 'Tarea'],
            ['text', 'responsible', 'Responsable'],
            ['date', 'startsAt', 'Fecha de inicio'],
            ['date', 'dueAt', 'Fecha de vencimiento'],
            ['number', 'budget', 'Presupuesto'],
        ];
        fields.forEach(([type, key, label], index) => {
            const cell = document.createElement('td');
            cell.append(inputFor(type, key, label, values[index] || ''));
            row.append(cell);
        });

        const statusCell = document.createElement('td');
        const status = document.createElement('span');
        status.className = 'text-muted';
        status.textContent = 'Pendiente';
        statusCell.append(status);
        row.append(statusCell);

        const actionCell = document.createElement('td');
        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.className = 'btn btn-outline-secondary';
        removeButton.textContent = 'Quitar';
        removeButton.addEventListener('click', () => {
            row.remove();
            updateSchedule();
        });
        actionCell.append(removeButton);
        row.append(actionCell);
        body.append(row);
    };

    const serialize = () => {
        if (!hidden) return;
        hidden.value = Array.from(body.rows).map(row =>
            ['task', 'responsible', 'startsAt', 'dueAt', 'budget']
                .map(key => row.querySelector(`[data-schedule-field="${key}"]`).value.trim())
                .join('; ')
        ).filter(line => line.split(';').some(value => value.trim())).join('\n');
    };

    const updateSchedule = () => {
        const total = Array.from(body.rows).reduce((sum, row) => {
            const amount = Number.parseFloat(row.querySelector('[data-schedule-field="budget"]').value);
            return sum + (Number.isFinite(amount) ? amount : 0);
        }, 0);
        const finalBudget = Number.parseFloat(budgetInput?.value || '');
        summary.textContent = `Suma de actividades: ${currency.format(total)} de ${currency.format(Number.isFinite(finalBudget) ? finalBudget : 0)}`;
        serialize();
    };

    const initial = hidden?.value.trim();
    if (initial) initial.split(/\r?\n/).forEach(line => addActivity(line.split(';').map(value => value.trim())));
    else addActivity();

    body.addEventListener('input', updateSchedule);
    addButton.addEventListener('click', () => {
        addActivity();
        updateSchedule();
    });
    budgetInput?.addEventListener('input', updateSchedule);
    scheduleEditor.closest('form')?.addEventListener('submit', serialize);
    updateSchedule();
}