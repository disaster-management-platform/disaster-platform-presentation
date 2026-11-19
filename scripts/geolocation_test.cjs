const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync(require('node:path').join(__dirname, '../wwwroot/js/site.js'), 'utf8');
function scenario({ saved = false, allowed = true, secure = true } = {}) {
    const fields = Object.fromEntries(['latitud', 'longitud', 'ubicacion'].map(name => [name, { value: saved ? ({ latitud: '4.123456789', longitud: '-75.123456789', ubicacion: 'Lugar conservado' })[name] : '' }]));
    const button = { addEventListener() {} }, status = {}, save = {};
    const form = { querySelector(selector) {
        if (selector === '[data-locate]') return button;
        if (selector === '[data-location-status]') return status;
        if (selector === 'button[type="submit"]') return save;
        return fields[selector.match(/Values\[(\w+)\]/)[1]];
    } };
    let calls = 0, point;
    const map = { setView() { return this; } };
    const L = { map: () => map, tileLayer: () => ({ addTo() {} }), marker: p => { point = p; return { addTo() { return this; }, setLatLng() {} }; } };
    const document = { getElementById: () => ({ dataset: { points: '[]' } }), querySelector: s => s === '[data-disaster-registration="true"]' ? form : null, querySelectorAll: () => [] };
    const navigator = { geolocation: { getCurrentPosition(success, failure, options) {
        calls++; assert.equal(options.maximumAge, 0); assert.equal(options.enableHighAccuracy, true);
        if (allowed) success({ coords: { latitude: 2.927301234, longitude: -75.281901234, accuracy: 8 } });
        else failure({ code: 1 });
    } } };
    vm.runInNewContext(source, { document, navigator, window: { L, isSecureContext: secure }, L });
    return { fields, status, save, calls, point };
}
let result = scenario();
assert.equal(result.fields.latitud.value, '2.927301234');
assert.equal(result.fields.longitud.value, '-75.281901234');
assert.match(result.fields.ubicacion.value, /2.927301, -75.281901/);
assert.equal(result.point[0], 2.927301234); assert.equal(result.point[1], -75.281901234);
assert.equal(result.save.disabled, false);
result = scenario({ allowed: false });
assert.equal(result.fields.latitud.value, ''); assert.equal(result.save.disabled, true);
assert.match(result.status.textContent, /Permite el acceso/);
result = scenario({ saved: true });
assert.equal(result.calls, 0); assert.equal(result.fields.ubicacion.value, 'Lugar conservado');
assert.equal(result.point[0], 4.123456789);
result = scenario({ secure: false });
assert.equal(result.calls, 0); assert.equal(result.save.disabled, true);
console.log('PASS GPS coordinates and map, permission denial, HTTPS requirement and preserved retry content');
