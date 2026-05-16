---
title: "B.12 — Test end-to-end manuale"
subtitle: "Smoke test del flusso rigenerazione magic-link impresa"
date: "2026-05-15"
---

# Pre-condizioni

- App attiva su http://localhost:5192 (PC) o http://192.168.1.107:5192 (telefono).
  - Avvio: `dotnet run --project src/ICMVerbali.Web --launch-profile lan`
  - IP corrente: `Get-NetIPAddress -AddressFamily IPv4 | Where { $_.InterfaceAlias -eq "Wi-Fi" }`
- Login `admin` / `AdminIcm2026!`
- Browser principale + una tab in **incognito** (per simulare l'Impresa)
- Un blocco note dove appuntare i magic-link emessi (LINK_A, LINK_B, LINK_C, LINK_X)

---

# Fase 1 — Crea il verbale e firma CSE

**1.1** Home → bottone **"Nuovo verbale"** → si apre il wizard.

**1.2 Step 1 (Anagrafica)** — compila:
- Data: lascia oggi
- Cantiere: scegline uno qualsiasi (es. *Lugagnano*)
- Committente, Impresa Appaltatrice, RL, CSP, CSE, DL: uno qualsiasi degli esistenti

→ "Avanti"

**1.3 Step 2 (Esito/meteo)** — Esito: *Conforme*; Meteo: *Sereno*; Temperatura: 20.

**1.4 Step 3-6** — puoi lasciare tutto vuoto/non applicabile (per il test di B.12 non serve nulla).

**1.5 Step 7 (Interferenze)** — *Nessuna*.

**1.6 Step 8 (Prescrizioni)** — lascia vuoto.

**1.7 Step 9 (Foto)** — lascia vuoto.

**1.8 Step 10 (Riepilogo)** → click **"Salva e firma"** → si apre `SignaturePadDialog`:
- Disegna due scarabocchi sul canvas
- "Conferma firma"

**Atteso**: il dialog si chiude, si apre subito **`LinkImpresaDialog`** con:
- Alert verde "Verbale N/2026 firmato dal CSE"
- Campo URL read-only del tipo `http://.../firma-impresa/{guid}`

**1.9** Click **"Copia link"** → snackbar "Link copiato negli appunti". **Salva questo URL in un blocco note** — lo chiameremo `LINK_A`.

**1.10** Click **"Vai alla home"**.

✅ **Checkpoint**: il verbale appare nella Home con stato *Firmato CSE*.

---

# Fase 2 — Detail view e blocco "Link firma Impresa"

**2.1** Click sul verbale appena creato → atterri su `/verbali/{id}`.

**Atteso**: scorrendo la pagina, **prima della sezione 10 (Firme)** trovi il nuovo blocco **"Link firma Impresa"** con:
- Testo "Condividi questo link... Scade il [data] [ora]..."
- Campo URL read-only — **deve coincidere con LINK_A**
- Bottoni *Copia link* (primario) + *Rigenera link* (outlined)

**2.2** Click **"Copia link"** → snackbar verde "Link copiato negli appunti". Incolla altrove per verificare che sia uguale a `LINK_A`.

✅ **Checkpoint 1**: il link mostrato in detail view è lo stesso emesso al momento della firma CSE.

---

# Fase 3 — Apri il vecchio link come Impresa

**3.1** Apri `LINK_A` in una **tab incognito** (per non confondersi con la sessione admin).

**Atteso**: pagina pubblica `/firma-impresa/{guid}` con:
- Header "ICM Verbali — Firma Impresa"
- Riepilogo del verbale (numero, anagrafica, esito)
- Anteprima della firma CSE
- Signature pad + campo "Nome firmatario" pre-compilato con la ragione sociale dell'Impresa

**Lascia questa tab aperta, NON firmare ancora.**

✅ **Checkpoint 2**: l'Impresa può aprire il link e vedere la pagina di firma.

---

# Fase 4 — Rigenera (il cuore di B.12)

**4.1** Torna sulla detail view (tab principale) → click **"Rigenera link"**.

**Atteso**: appare `MessageBox` di conferma:
> "Il link attualmente attivo verrà invalidato. Eventuali copie già inviate all'Impresa smetteranno di funzionare. Procedere?"

con bottoni **Rigenera** / **Annulla**.

**4.2** Prova prima **Annulla** → niente succede, `LINK_A` resta valido (puoi verificare ricaricando la tab incognito: deve ancora funzionare).

**4.3** Click di nuovo **"Rigenera link"** → questa volta conferma **Rigenera**.

**Atteso**:
- Si apre `LinkImpresaDialog` con titolo *"Nuovo link verbale N/2026"*
- Il link nel campo è **diverso** da `LINK_A` — chiamalo `LINK_B`
- Click *"Copia link"* → snackbar di conferma

**4.4** Click **"Vai alla home"** → torni alla detail view.

**Atteso**: il blocco "Link firma Impresa" ora mostra `LINK_B` (non più `LINK_A`).

✅ **Checkpoint 3**: la rigenerazione produce un nuovo URL e la detail view si aggiorna.

---

# Fase 5 — Il vecchio link è sostituito

**5.1** Torna alla tab incognito (quella aperta su `LINK_A`) → premi **F5** (ricarica).

**Atteso**: pagina di errore con:
- Titolo **"Link sostituito"**
- Messaggio **"Il CSE ha generato un nuovo link per questo verbale. Richiedi quello aggiornato."**

✅ **Checkpoint 4 (il più importante)**: l'Impresa che apre il vecchio link riceve il messaggio corretto, non un generico "scaduto".

---

# Fase 6 — Doppia rigenerazione (race tra due tab)

**6.1** Sulla detail view → "Rigenera link" → conferma → arriva `LINK_C`. Vai alla home, torna alla detail view.

**6.2** Verifica che la detail view mostri `LINK_C`.

**6.3** Apri `LINK_B` in incognito → deve mostrare **"Link sostituito"** (è stato revocato dalla nuova rigenerazione).

✅ **Checkpoint 5**: rigenerare a catena revoca tutti i precedenti, solo l'ultimo è valido.

---

# Fase 7 — Firma Impresa col link buono

**7.1** Apri `LINK_C` in una tab incognito nuova (chiudi la precedente per pulizia).

**Atteso**: pagina di firma normale (l'ultimo link emesso è attivo).

**7.2** Disegna la firma → "Conferma firma".

**Atteso**: messaggio di conferma "Firma registrata".

**7.3** Torna sulla detail view (tab principale) e ricarica.

**Atteso**:
- Il chip di stato in alto è cambiato da *Firmato CSE* a *Firmato Impresa*
- Il blocco **"Link firma Impresa" è sparito** (compare solo per stato `FirmatoCse`)
- Sotto, in sezione 10, ora compaiono **entrambe le firme** (CSE + Impresa)

✅ **Checkpoint 6**: con un link revocato (`LINK_A` o `LINK_B`) che venisse riaperto ora, il messaggio sarebbe ancora "Link sostituito"; con `LINK_C` riaperto sarebbe "Link già utilizzato" (cambia il motivo, perché ora è stato consumato).

---

# Fase 8 (bonus) — Difesa contro l'Impresa che firma con link revocato

Per simulare la race "Impresa apre il link, CSE rigenera mentre lei sta disegnando":

**8.1** Crea un altro verbale ripetendo la Fase 1 → ottieni `LINK_X`.

**8.2** Apri `LINK_X` in incognito → arriva alla pagina di firma → **disegna ma NON premere ancora "Conferma"**.

**8.3** In una tab principale (admin), apri la detail view del nuovo verbale → **Rigenera link** → conferma.

**8.4** Torna alla tab incognito → premi **"Conferma firma"** sul disegno già pronto.

**Atteso**: errore — la firma viene rifiutata perché il token è stato revocato nel frattempo. Lo stato del verbale resta `FirmatoCse`.

✅ **Checkpoint 7**: la difesa lato repository (`SqlMarkTokenUsato` con `RevocatoUtc IS NULL`) funziona end-to-end, anche se il manager fosse aggirato.
