const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, '../wwwroot/js/site.js'), 'utf8');
const settle = () => new Promise(resolve => setImmediate(resolve));
async function scenario({ saved = false, allowed = true, secure = true, geocoder = true } = {}) {
    const input = value => ({ value, handlers: {}, addEventListener(name, action) { this.handlers[name] = action; } });
    const fields = Object.fromEntries(['latitud', 'longitud', 'ubicacion', 'disasterTypeId', 'startedAt', 'targetVersion'].map(name => [name, input('')]));
    fields.disasterTypeId.value = '2'; fields.startedAt.value = '2026-11-19T10:00';
    if (saved) Object.assign(fields, { latitud: input('4.123456789'), longitud: input('-75.123456789'), ubicacion: input('Lugar conservado') });
    const button = input(''), status = {}, save = {}, matchStatus = {};
    const selection = Object.assign(input(''), { dataset: {}, options: [], replaceChildren(option) { this.options = [option]; this.value = option.value; }, append(option) { this.options.push(option); } });
    Object.defineProperty(selection, 'selectedOptions', { get() { return this.options.filter(x => x.value === this.value); } });
    const form = { dataset: { geocodingUrl: 'https://geocoder.test/reverse' }, querySelector(selector) {
        if (selector === '[data-locate]') return button;
        if (selector === '[data-location-status]') return status;
        if (selector === '[data-match-status]') return matchStatus;
        if (selector === '[data-disaster-match]') return selection;
        if (selector === 'button[type="submit"]') return save;
        return fields[selector.match(/Values\[(\w+)\]/)[1]];
    } };
    let calls = 0, point, candidates = [{ id: 4, version: 3, nombre: 'Evento existente', ubicacion: 'Neiva, Huila', startedAt: '2026-11-19T12:00:00Z' }];
    const map = { setView() { return this; } };
    const L = { map: () => map, tileLayer: () => ({ addTo() {} }), marker: p => { point = p; return { addTo() { return this; }, setLatLng() {} }; } };
    const document = { getElementById: () => ({ dataset: { points: '[]' } }), querySelector: s => s === '[data-disaster-registration="true"]' ? form : null, querySelectorAll: () => [] };
    const navigator = { geolocation: { getCurrentPosition(success, failure, options) {
        calls++; assert.equal(options.maximumAge, 0); assert.equal(options.enableHighAccuracy, true);
        if (allowed) success({ coords: { latitude: 2.927301234, longitude: -75.281901234, accuracy: 8 } });
        else failure({ code: 1 });
    } } };
    function Option(text, value) { this.text = text; this.value = value; this.dataset = {}; }
    const fetch = async (url, options) => {
        if (String(url).startsWith('https://geocoder.test/')) {
            assert.equal(options.credentials, 'omit'); assert.equal(options.headers, undefined);
            if (!geocoder) throw new Error('Unavailable');
            const query = new URL(url).searchParams;
            return { ok: true, json: async () => ({ city: 'Neiva', principalSubdivision: 'Huila', lookupSource: 'coordinates', latitude: Number(query.get('latitude')), longitude: Number(query.get('longitude')) }) };
        }
        assert.equal(options.credentials, 'same-origin'); assert.ok(String(url).includes('disasterTypeId=2'));
        return { ok: true, json: async () => candidates };
    };
    vm.runInNewContext(source, { document, navigator, window: { L, isSecureContext: secure }, L, URL, URLSearchParams, AbortSignal, fetch, Option });
    await settle(); await settle();
    return { fields, status, save, selection, calls, point, candidates: value => { candidates = value; } };
}
(async () => {
    let result = await scenario();
    assert.equal(result.fields.latitud.value, '2.927301234'); assert.equal(result.fields.longitud.value, '-75.281901234');
    assert.equal(result.fields.ubicacion.value, 'Neiva, Huila'); assert.equal(result.point[0], 2.927301234); assert.equal(result.point[1], -75.281901234);
    assert.equal(result.selection.value, ''); assert.equal(result.selection.options.length, 2); assert.equal(result.save.disabled, false);
    result.selection.value = '4'; result.selection.handlers.change(); assert.equal(result.fields.targetVersion.value, '3');
    result.candidates([]); await result.fields.startedAt.handlers.change(); await settle();
    assert.equal(result.selection.value, '4'); assert.equal(result.save.disabled, true);
    result = await scenario({ allowed: false }); assert.equal(result.fields.latitud.value, ''); assert.equal(result.save.disabled, true); assert.match(result.status.textContent, /Permite el acceso/);
    result = await scenario({ saved: true }); assert.equal(result.calls, 0); assert.equal(result.fields.ubicacion.value, 'Lugar conservado'); assert.equal(result.point[0], 4.123456789);
    result = await scenario({ secure: false }); assert.equal(result.calls, 0); assert.equal(result.save.disabled, true);
    result = await scenario({ geocoder: false }); assert.equal(result.fields.ubicacion.value, ''); assert.equal(result.save.disabled, true); assert.match(result.status.textContent, /escríbelo/);
    console.log('PASS GPS/map/locality, explicit suggestions and version, lost-scope protection, permissions, HTTPS, retries and geocoder failure');
})().catch(error => { console.error(error); process.exitCode = 1; });
