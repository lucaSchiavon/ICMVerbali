// Copia di una stringa negli appunti con fallback per contesti non-sicuri (http LAN).
// navigator.clipboard.writeText e' disponibile solo in secure context (https o localhost):
// quando l'app gira su http LAN (es. test da smartphone su 192.168.x.y), serve il fallback
// document.execCommand('copy') via textarea temporanea.
// Ritorna true se la copia e' andata a buon fine.
export async function copy(text) {
    if (navigator.clipboard && window.isSecureContext) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            // Cade nel fallback sotto.
        }
    }

    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "fixed";
    textarea.style.top = "0";
    textarea.style.left = "0";
    textarea.style.opacity = "0";
    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();
    textarea.setSelectionRange(0, textarea.value.length);

    let ok = false;
    try {
        ok = document.execCommand("copy");
    } catch {
        ok = false;
    }
    document.body.removeChild(textarea);
    return ok;
}
