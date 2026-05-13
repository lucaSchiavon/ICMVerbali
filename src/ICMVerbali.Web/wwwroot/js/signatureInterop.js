// Wrapper JS interop per signature_pad (vedi /lib/signature_pad/signature_pad.umd.min.js).
// Caricato dinamicamente con IJSObjectReference: ogni canvas usa una sua istanza
// SignaturePad mantenuta in una WeakMap canvas->pad per evitare leak.

const pads = new WeakMap();
let libPromise = null;

function ensureLibLoaded() {
    if (window.SignaturePad) return Promise.resolve();
    if (libPromise) return libPromise;
    libPromise = new Promise((resolve, reject) => {
        const s = document.createElement("script");
        s.src = "/lib/signature_pad/signature_pad.umd.min.js";
        s.async = true;
        s.onload = () => resolve();
        s.onerror = () => reject(new Error("signature_pad load failed"));
        document.head.appendChild(s);
    });
    return libPromise;
}

function resizeCanvas(canvas) {
    const ratio = Math.max(window.devicePixelRatio || 1, 1);
    const rect = canvas.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) return;
    canvas.width = rect.width * ratio;
    canvas.height = rect.height * ratio;
    const ctx = canvas.getContext("2d");
    ctx.scale(ratio, ratio);
}

export async function init(canvasId, options) {
    await ensureLibLoaded();
    const canvas = document.getElementById(canvasId);
    if (!canvas) throw new Error(`canvas '${canvasId}' not found`);

    resizeCanvas(canvas);
    const pad = new window.SignaturePad(canvas, {
        backgroundColor: (options && options.backgroundColor) || "rgb(255,255,255)",
        penColor: (options && options.penColor) || "rgb(0,0,0)",
        minWidth: 0.6,
        maxWidth: 2.2,
    });
    pad.clear();
    pads.set(canvas, pad);
}

export function clear(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const pad = pads.get(canvas);
    if (pad) pad.clear();
}

export function isEmpty(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return true;
    const pad = pads.get(canvas);
    return pad ? pad.isEmpty() : true;
}

export function getDataUrl(canvasId, mime) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;
    const pad = pads.get(canvas);
    if (!pad || pad.isEmpty()) return null;
    return pad.toDataURL(mime || "image/png");
}

export function dispose(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const pad = pads.get(canvas);
    if (pad) {
        pad.off();
        pads.delete(canvas);
    }
}
