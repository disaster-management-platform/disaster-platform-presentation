// Mapa Leaflet (dashboards y detalle de desastre)
const mapEl = document.getElementById('map');
if (mapEl && window.L) {
    const pts = JSON.parse(mapEl.dataset.points || '[]');
    const map = L.map(mapEl).setView([4.6, -75.6], 7);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 18, attribution: '&copy; OpenStreetMap'
    }).addTo(map);
    const color = s => (s >= 4 ? '#dc3545' : s === 3 ? '#fd7e14' : '#1f5fa8');
    const bounds = [];
    pts.forEach(p => {
        if (!Number.isFinite(p.lat) || !Number.isFinite(p.lng)) return;
        const title = document.createElement('span');
        title.textContent = p.name;
        L.marker([p.lat, p.lng]).addTo(map).bindPopup(title);
        if (Number.isFinite(p.radio) && p.radio > 0) L.circle([p.lat, p.lng], { radius: p.radio, color: color(p.sev) }).addTo(map);
        if (p.polygon) L.geoJSON(p.polygon).addTo(map);
        bounds.push([p.lat, p.lng]);
    });
    if (bounds.length > 1) map.fitBounds(bounds, { padding: [30, 30] });
    else if (bounds.length === 1) map.setView(bounds[0], 10);
}

document.querySelectorAll('[data-interactive-chart]').forEach(root => {
    const sourceValues = JSON.parse(root.dataset.chartValues || '[]');
    const rankedValues = [...sourceValues].sort((left, right) => right.value - left.value);
    const values = sourceValues.length > 7
        ? [
            ...rankedValues.slice(0, 7),
            { label: 'Otros', value: rankedValues.slice(7).reduce((sum, item) => sum + item.value, 0) }
        ]
        : sourceValues;
    const colorSlots = values.map((_, index) => index);
    const canvas = root.querySelector('[data-chart-canvas]');
    const legend = root.querySelector('[data-chart-legend]');
    const summary = root.querySelector('[data-chart-summary]');
    const controls = root.closest('.card')?.querySelectorAll('button[data-chart-type]') || [];
    const title = root.dataset.chartTitle || 'Gráfico';
    const numberFormat = new Intl.NumberFormat('es-CO');
    let chartType = root.dataset.chartType || 'bars';
    let selectedIndex = null;

    const svgElement = (name, attributes = {}) => {
        const element = document.createElementNS('http://www.w3.org/2000/svg', name);
        Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
        return element;
    };
    const addText = (svg, text, x, y, className, anchor) => {
        const element = svgElement('text', { x, y, class: className });
        if (anchor) element.setAttribute('text-anchor', anchor);
        element.textContent = text;
        svg.append(element);
        return element;
    };
    const addTitle = (element, text) => {
        const tooltip = svgElement('title');
        tooltip.textContent = text;
        element.append(tooltip);
    };
    const shortLabel = (label, length) =>
        label.length > length ? `${label.slice(0, length - 1)}…` : label;
    const maxValue = Math.max(1, ...values.map(item => Math.max(0, item.value)));

    const createSvg = (viewBox, type) => {
        const svg = svgElement('svg', {
            class: `chart-svg chart-svg--${type}`,
            viewBox,
            role: 'img',
            'aria-label': title
        });
        const description = svgElement('title');
        description.textContent = title;
        svg.append(description);
        return svg;
    };

    const renderBars = () => {
        const rowHeight = 42;
        const svg = createSvg(`0 0 900 ${Math.max(180, values.length * rowHeight + 20)}`, 'bars');
        values.forEach((item, index) => {
            const y = 10 + index * rowHeight;
            const group = svgElement('g', { 'data-chart-mark': index });
            addTitle(group, `${item.label}: ${numberFormat.format(item.value)}`);
            addText(group, shortLabel(item.label, 25), 0, y + 23, 'chart-label');
            group.append(svgElement('rect', {
                x: 225, y: y + 5, width: Math.max(0, item.value) / maxValue * 590, height: 24,
                rx: 2, class: `chart-color-${colorSlots[index]}`
            }));
            addText(group, numberFormat.format(item.value), 830, y + 23, 'chart-value');
            svg.append(group);
        });
        return svg;
    };

    const renderColumns = () => {
        const width = 900;
        const top = 24;
        const bottom = 265;
        const left = 64;
        const right = 885;
        const plotHeight = bottom - top;
        const slotWidth = (right - left) / Math.max(1, values.length);
        const svg = createSvg(`0 0 ${width} 335`, 'columns');

        for (let step = 0; step <= 4; step++) {
            const fraction = step / 4;
            const y = bottom - fraction * plotHeight;
            svg.append(svgElement('line', {
                x1: left, y1: y, x2: right, y2: y, class: 'chart-grid'
            }));
            addText(svg, numberFormat.format(Math.round(maxValue * fraction)), left - 8, y + 5, 'chart-label', 'end');
        }

        values.forEach((item, index) => {
            const x = left + index * slotWidth + slotWidth / 2;
            const barHeight = Math.max(0, item.value) / maxValue * plotHeight;
            const group = svgElement('g', { 'data-chart-mark': index });
            addTitle(group, `${item.label}: ${numberFormat.format(item.value)}`);
            group.append(svgElement('rect', {
                x: x - Math.min(54, slotWidth * 0.58) / 2,
                y: bottom - barHeight,
                width: Math.min(54, slotWidth * 0.58),
                height: barHeight,
                rx: 2,
                class: `chart-color-${colorSlots[index]}`
            }));
            addText(group, numberFormat.format(item.value), x, Math.max(top + 14, bottom - barHeight - 8), 'chart-value', 'middle');
            addText(group, shortLabel(item.label, 13), x, bottom + 26, 'chart-label', 'middle');
            svg.append(group);
        });
        return svg;
    };

    const renderPie = () => {
        const svg = createSvg('0 0 800 340', 'pie');
        const centerX = 400;
        const centerY = 165;
        const radius = 130;
        const total = values.reduce((sum, item) => sum + Math.max(0, item.value), 0);
        let startAngle = -Math.PI / 2;

        values.forEach((item, index) => {
            const sweep = total > 0 ? Math.max(0, item.value) / total * Math.PI * 2 : 0;
            const endAngle = startAngle + sweep;
            const group = svgElement('g', { 'data-chart-mark': index });
            const startX = centerX + radius * Math.cos(startAngle);
            const startY = centerY + radius * Math.sin(startAngle);
            const endX = centerX + radius * Math.cos(endAngle);
            const endY = centerY + radius * Math.sin(endAngle);
            const path = svgElement('path', {
                d: sweep >= Math.PI * 2
                    ? ''
                    : `M ${centerX} ${centerY} L ${startX} ${startY} A ${radius} ${radius} 0 ${sweep > Math.PI ? 1 : 0} 1 ${endX} ${endY} Z`,
                class: `chart-color-${colorSlots[index]}`,
                stroke: '#fff',
                'stroke-width': 2
            });
            addTitle(group, `${item.label}: ${numberFormat.format(item.value)}`);
            if (sweep >= Math.PI * 2) {
                group.append(svgElement('circle', {
                    cx: centerX, cy: centerY, r: radius, class: `chart-color-${colorSlots[index]}`
                }));
            } else if (sweep > 0) {
                group.append(path);
            }
            svg.append(group);
            startAngle = endAngle;
        });
        return svg;
    };

    const updateSelection = () => {
        canvas.querySelectorAll('[data-chart-mark]').forEach(mark => {
            mark.setAttribute('opacity', selectedIndex === null || Number(mark.dataset.chartMark) === selectedIndex ? '1' : '0.28');
        });
        legend.querySelectorAll('[data-chart-index]').forEach(button => {
            button.setAttribute('aria-pressed', Number(button.dataset.chartIndex) === selectedIndex ? 'true' : 'false');
        });
        summary.textContent = selectedIndex === null
            ? 'Selecciona una categoría para resaltarla.'
            : `${values[selectedIndex].label}: ${numberFormat.format(values[selectedIndex].value)}`;
    };

    const render = () => {
        const svg = chartType === 'pie' ? renderPie() : chartType === 'columns' ? renderColumns() : renderBars();
        canvas.replaceChildren(svg);
        controls.forEach(button => {
            const active = button.dataset.chartType === chartType;
            button.setAttribute('aria-pressed', active ? 'true' : 'false');
            button.classList.toggle('btn-secondary', active);
            button.classList.toggle('btn-outline-secondary', !active);
        });
        updateSelection();
    };

    values.forEach((item, index) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-secondary chart-legend__item';
        button.dataset.chartIndex = index;
        button.setAttribute('aria-pressed', 'false');
        button.title = `${item.label}: ${numberFormat.format(item.value)}`;

        const swatch = document.createElement('span');
        swatch.className = `chart-legend__swatch chart-swatch--${colorSlots[index]}`;
        swatch.setAttribute('aria-hidden', 'true');
        const label = document.createElement('span');
        label.textContent = item.label;
        const value = document.createElement('span');
        value.className = 'fw-semibold';
        value.textContent = numberFormat.format(item.value);
        button.append(swatch, label, value);
        button.addEventListener('click', () => {
            selectedIndex = selectedIndex === index ? null : index;
            updateSelection();
        });
        legend.append(button);
    });

    controls.forEach(button => button.addEventListener('click', () => {
        chartType = button.dataset.chartType;
        render();
    }));
    render();
});

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