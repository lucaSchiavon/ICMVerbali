---
title: "B.11 — Firma Impresa Appaltatrice via magic-link"
subtitle: "Guida all'implementazione del flusso B.11"
author: "Progetto ICMVerbali"
date: "2026-05-14"
---

# Introduzione

Questo documento estende [B10-firma-implementazione](B10-firma-implementazione.md) descrivendo la sotto-fase **B.11**: la firma dell'**Impresa Appaltatrice** sul verbale di sopralluogo, dopo la firma del CSE.

In B.10 abbiamo introdotto la firma elettronica semplice del CSE (canvas + signature_pad JS) e la transizione `Bozza → FirmatoCse`. B.11 chiude il ciclo aggiungendo la transizione `FirmatoCse → FirmatoImpresa` tramite un **magic-link**: un URL contenente un token GUID v7 che il CSE genera al momento della propria firma e condivide manualmente con il referente dell'Impresa (via WhatsApp, email, SMS, a sua scelta). Il referente apre il link sul proprio dispositivo, senza creare un account, firma su un canvas analogo a quello del CSE e il verbale passa a `FirmatoImpresa`.

Il documento assume la lettura di B.10 e di `docs/01-design.md` (Addendum 2026-05-14). Qui ci concentriamo su **cosa è cambiato rispetto a B.10** e sulle **scelte di sicurezza** del magic-link.

---

# 1. Cosa abbiamo costruito in B.11

Tre cose nuove, tutte additive rispetto a B.10:

1. **Tabella `FirmaToken`** (migration `002_AddFirmaToken.sql`): un token monouso con scadenza per ogni verbale firmato dal CSE.
2. **Pagina pubblica `/firma-impresa/{token}`**: accessibile senza login, mostra un riepilogo del verbale (più la firma CSE renderizzata inline) e un signature pad per la firma Impresa.
3. **Detail view `/verbali/{id}`**: pagina autenticata read-only che mostra un verbale firmato (numero, anagrafica, checklist, prescrizioni, foto, **entrambe le firme renderizzate**), con bottoni "Indietro" e "Stampa".

In più due cambiamenti minori:

- `VerbaleRepository.FirmaCseAsync` ora **emette anche il token** nella stessa transazione DB (invariante: ogni verbale `FirmatoCse` ha il proprio magic-link pronto all'uso).
- Lo step 10 del wizard, dopo la firma CSE, non torna più direttamente alla home: apre prima un dialog "Link impresa" con il URL completo e un bottone **Copia link**.

---

# 2. Flusso end-to-end

Dalla prospettiva dei due utenti.

## CSE (autenticato sul sistema)

1. Compila il wizard del verbale (step 1–9).
2. Sullo step 10, clicca **"Salva e firma"** → si apre `SignaturePadDialog` (lo stesso di B.10).
3. Disegna la firma, clicca "Conferma firma".
4. Il backend in **una transazione** assegna `Numero/Anno`, inserisce la riga `Firma` (CSE), passa lo stato a `FirmatoCse`, scrive l'audit **e inserisce un `FirmaToken`**. Il dialog di firma si chiude.
5. Si apre `LinkImpresaDialog` con il messaggio "Verbale N/AAAA firmato dal CSE" e un campo testo (read-only) contenente l'URL `https://{host}/firma-impresa/{token}`.
6. CSE clicca **"Copia link"** (`navigator.clipboard.writeText`), incolla in WhatsApp/email/SMS al referente impresa, clicca "Vai alla home".

## Impresa Appaltatrice (anonima, no account)

1. Riceve il link e lo apre nel browser (tipicamente dal proprio smartphone).
2. La pagina `/firma-impresa/{token}` valida il token: se scaduto/usato/inesistente mostra una pagina di errore con messaggio specifico.
3. Se OK, la pagina mostra un riepilogo del verbale (cantiere, data, parti, esito) e l'**anteprima della firma CSE** già apposta (renderizzata inline come `data:image/png;base64,...`, vedi §4).
4. Sotto il riepilogo c'è un signature pad analogo a quello del CSE, con il campo "Nome del firmatario" pre-compilato con la ragione sociale dell'Impresa (editabile).
5. Disegna la firma, clicca "Conferma firma".
6. Il backend, **in una transazione**, valida il token (scaduto/usato/inesistente → eccezione tipizzata `FirmaTokenInvalidoException`), inserisce la riga `Firma` (Impresa), passa lo stato a `FirmatoImpresa`, scrive l'audit, **marca il token come `UsatoUtc = SYSUTCDATETIME()`** (uso singolo).
7. La pagina mostra "Firma registrata. Grazie." e l'utente può chiudere il browser.

## Dopo

- Il CSE, tornato alla Home, vede il verbale con stato `Firmato Impresa`.
- Click sul verbale → apre `/verbali/{id}` (detail view), dove può rivedere tutti i dati e le due firme renderizzate, oppure stampare.

---

# 3. Modello dati: la tabella `FirmaToken`

```sql
CREATE TABLE dbo.FirmaToken
(
    Id              uniqueidentifier NOT NULL PRIMARY KEY,
    VerbaleId       uniqueidentifier NOT NULL REFERENCES dbo.Verbale (Id) ON DELETE CASCADE,
    Token           uniqueidentifier NOT NULL UNIQUE,
    ScadenzaUtc     datetime2(0)     NOT NULL,
    UsatoUtc        datetime2(0)     NULL,
    CreatedAt       datetime2(3)     NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_FirmaToken_VerbaleId ON dbo.FirmaToken (VerbaleId);
```

Tre note:

- **`Id` distinto da `Token`**: `Id` è la PK interna (uso in `UPDATE ... WHERE Id = @TokenId`). `Token` è il GUID che appare nell'URL. Sono entrambi GUID v7 ma indipendenti: questo permette di non esporre la PK nell'URL e di poter, in futuro, ruotare un token (rigenerarne uno nuovo) senza cambiare `Id`.
- **`UsatoUtc` nullable**: rappresenta lo "stato" del token. `NULL` = vergine, valore = consumato. L'UPDATE che marca un token come usato ha la condizione `WHERE Id = @Id AND UsatoUtc IS NULL`: se due tab firmano insieme, la seconda riceve 0 righe modificate e abortisce la transazione.
- **`ON DELETE CASCADE`**: se in dev cancelliamo un verbale, i suoi token (orfani per definizione) spariscono.

---

# 4. La pagina `/firma-impresa/{token}`

Il punto più delicato di B.11 è la pagina pubblica. Quattro decisioni da motivare.

## 4.1 Niente `[Authorize]`

L'impresa non ha un account. La pagina ha `@page "/firma-impresa/{Token:guid}"` e nessun `[Authorize]`. Il routing di Blazor Server la rende accessibile a chiunque conosca il token.

Per evitare di mostrare il chrome del backoffice (NavMenu, logout, ecc.), la pagina dichiara `@layout PublicLayout`, un layout dedicato con sola `MudAppBar` di branding.

## 4.2 Validazione "early" del token

L'`OnInitializedAsync` della pagina chiama `IFirmaTokenManager.ValidaTokenAsync(token)`, che può lanciare `FirmaTokenInvalidoException` con un `Motivo`: `NonTrovato`, `Scaduto`, `GiaUsato`. La pagina cattura l'eccezione e mostra una *pagina di errore* dedicata con il messaggio specifico (vedi `TokenErrorTitle` / `TokenErrorMessage` in `FirmaImpresa.razor`).

Stessa cosa se il verbale associato al token è in stato diverso da `FirmatoCse` (perché già firmato impresa, o perché un rollback manuale lo ha riportato a Bozza): l'utente vede "Link non valido" senza ulteriori dettagli, per non rivelare lo stato interno.

## 4.3 Anteprima della firma CSE: inline base64, non endpoint pubblico

La pagina mostra all'impresa la firma del CSE già apposta (così l'impresa "vede chi ha firmato per primo"). L'endpoint `/api/firme/{id}/{tipo}` però è `RequireAuthorization()` — l'impresa non potrebbe accedervi.

Due opzioni:

1. **Endpoint pubblico** `/api/firme-token/{token}/cse` che valida il token prima di servire l'immagine.
2. **Inline base64**: il server-rendering della pagina legge il PNG via `IFirmaStorageService` e lo serializza in un `data:image/png;base64,...` dentro la `<img src>`.

Abbiamo scelto (2). Razionale: zero superficie d'attacco aggiuntiva (nessun nuovo endpoint pubblico), il PNG di una firma pesa ~20 KB → base64 ~27 KB nel DOM, perfettamente accettabile.

```csharp
await using var stream = await FirmaStorage.ApriLetturaAsync(firmaCse.ImmagineFirmaPath);
using var ms = new MemoryStream();
await stream.CopyToAsync(ms);
_firmaCseBase64 = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
```

## 4.4 Riuso del modulo JS `signatureInterop.js`

Il signature pad per l'impresa è lo stesso di B.10: stesso file JS `wwwroot/js/signatureInterop.js`, stesso pattern `IJSObjectReference` per `init/clear/isEmpty/getDataUrl/dispose`, stesso canvas 200px di altezza con `touch-action: none`. Niente codice JS nuovo. La sola differenza C# è che il submit chiama `VerbaleManager.FirmaImpresaAsync(token, nome, pngBytes)` invece di `FirmaCseAsync`.

---

# 5. Atomicità: token nella stessa transazione della firma CSE

Decisione importante. Il `FirmaToken` viene creato **dentro la transazione SQL** di `VerbaleRepository.FirmaCseAsync`, non in un secondo step post-commit:

```csharp
// 6. INSERT FirmaToken (B.11): atomico con la firma CSE. Garantisce
// che ogni verbale FirmatoCse abbia immediatamente un magic-link
// utilizzabile dall'Impresa.
await conn.ExecuteAsync(new CommandDefinition(SqlInsertFirmaToken, new
{
    Id = tokenImpresa.TokenId,
    VerbaleId = verbaleId,
    Token = tokenImpresa.Token,
    ScadenzaUtc = tokenImpresa.ScadenzaUtc,
    CreatedAt = tokenImpresa.CreatedAt,
}, transaction: tx, cancellationToken: ct));
```

Il manager pre-calcola i valori del token (`Guid.CreateVersion7()`, scadenza = now + `ScadenzaOreDefault` dalle options) e li passa al repository come `FirmaTokenInputs`. Così il repository fa solo SQL e mantiene tutta la transazione (verbale + firma + audit + token) coerente:

| Step transazione | Operazione |
| --- | --- |
| 1 | `SELECT Stato WITH (UPDLOCK, HOLDLOCK)` + verifica `Stato == Bozza` |
| 2 | `SELECT MAX(Numero) WITH (UPDLOCK, HOLDLOCK) WHERE Anno = @Anno` → +1 |
| 3 | `INSERT INTO Firma` (CSE) |
| 4 | `UPDATE Verbale SET Stato=FirmatoCse, Numero, Anno, UpdatedAt` |
| 5 | `INSERT INTO VerbaleAudit` (EventoTipo=Firma) |
| 6 | `INSERT INTO FirmaToken` |
| commit | tutto-o-niente |

Se uno qualsiasi degli step fallisce, il rollback ripristina la bozza e nessun token resta orfano nel DB.

---

# 6. Transazione `FirmatoCse → FirmatoImpresa`

Speculare ma più semplice: nessuna assegnazione `Numero/Anno`, nessun `MAX(...)`. La sequenza in `VerbaleRepository.FirmaImpresaAsync`:

| Step | Operazione |
| --- | --- |
| 1 | `SELECT Stato WITH (UPDLOCK, HOLDLOCK)` + verifica `Stato == FirmatoCse` |
| 2 | `UPDATE FirmaToken SET UsatoUtc = SYSUTCDATETIME() WHERE Id = @TokenId AND UsatoUtc IS NULL`. **Se 0 righe modificate, throw**: il token è già stato consumato da un'altra tab. |
| 3 | `INSERT INTO Firma` con `Tipo = ImpresaAppaltatrice` |
| 4 | `UPDATE Verbale SET Stato=FirmatoImpresa, UpdatedAt` (Numero/Anno **non cambiano**) |
| 5 | `INSERT INTO VerbaleAudit` (`EventoTipo=Firma`, Note=`"Firma applicata da Impresa via token {tokenId}: {nome}"`) |
| commit | tutto-o-niente |

Il `MarkTokenUsato` allo step 2 è la **difesa contro la race** "due tab aperte sullo stesso link" (entrambe vedono UsatoUtc=null al momento del rendering, entrambe submittono). Solo una delle due transazioni può fare l'UPDATE con 0 righe-zero: il vincolo `WHERE UsatoUtc IS NULL` viene valutato dopo i lock, e solo la prima transazione vede `UsatoUtc=null`. La seconda fa `rows=0` → eccezione → rollback completo.

## L'audit dell'impresa: `UtenteId = CompilatoDaUtenteId`

L'impresa non ha account, quindi non ha un `UtenteId` da scrivere nella tabella `VerbaleAudit` (la cui FK è verso `Utente`). Tre opzioni considerate:

1. **Rendere `VerbaleAudit.UtenteId` nullable**: migration su FK esistente, invasiva.
2. **Utente di sistema "Impresa"**: serve un seed, complica le query di audit.
3. **`UtenteId = verbale.CompilatoDaUtenteId`** (l'utente CSE che ha creato il verbale), con `Note = "Firma applicata da Impresa via token {tokenId}: {nome}"`.

Abbiamo scelto (3). Semanticamente l'audit dice "evento firma originato dal flusso compilato da utente X, di tipo Impresa via token Y, firmatario Z". Niente schema change, niente seed.

---

# 7. Sicurezza del magic-link

Il token è una *capability*: chi lo possiede può firmare. Vincoli sovrapposti:

1. **Imprevedibilità**: GUID v7 a 128 bit, generato server-side. Probabilità di brute-force trascurabile.
2. **Scadenza**: 48h (configurabile via `FirmaTokenOptions.ScadenzaOreDefault` in `appsettings.json`).
3. **Uso singolo**: `UsatoUtc` valorizzato dopo la firma, l'UPDATE è condizionato a `UsatoUtc IS NULL`.
4. **Stato del verbale**: la pagina valida `Stato == FirmatoCse` prima di mostrare il signature pad.
5. **HTTPS in produzione** (già da B.10 con `UseHsts`).

Rischi accettati:

- **Inoltro del link**: chi inoltra il link concede la capability. Stesso rischio di un foglio cartaceo passato in azienda. Non risolvibile senza autenticazione.
- **Identita' del firmatario impresa**: non è verificata dal sistema. È regolata dal contratto tra CSE e impresa.

Non è implementato un **rate limit** specifico sul path `/firma-impresa/{token}` perché l'imprevedibilità del token è la difesa primaria. Da rivalutare in osservazione produzione.

---

# 8. Test

Stack di test invariato (xUnit, integration su SQL Server locale `ICMVerbaliDb`).

## 8.1 Test sul repository (`VerbaleRepositoryTests`)

Nuovi tre:

- **`FirmaCseAsync_inserisce_anche_FirmaToken_con_UsatoUtc_null`**: verifica che dopo la firma CSE esista una riga in `FirmaToken` con `UsatoUtc IS NULL` e che il `TokenImpresa` ritornato corrisponda al `Token` passato in input.
- **`FirmaImpresaAsync_su_FirmatoCse_passa_a_FirmatoImpresa_e_marca_token_usato`**: end-to-end: crea bozza, firma CSE, firma impresa. Verifica stato → `FirmatoImpresa`, `Numero/Anno` invariati, riga `Firma` Impresa esistente, `UsatoUtc` valorizzato, 3 righe audit (Creazione + 2 Firma).
- **`FirmaImpresaAsync_su_verbale_in_bozza_lancia_InvalidOperationException`**: difesa stato sbagliato.
- **`FirmaImpresaAsync_token_gia_usato_lancia_InvalidOperationException`**: forziamo `UsatoUtc != null` con UPDATE manuale, poi tentiamo la firma → eccezione, stato del verbale invariato (`FirmatoCse`).

I 3 test FirmaCseAsync pre-esistenti sono stati aggiornati con il nuovo parametro `FirmaTokenInputs` tramite un helper `BuildTokenInputs()`.

## 8.2 Test sul manager (`FirmaTokenManagerTests`)

Nuovi cinque, **unit puri** (niente DB): `FakeRepo` in-memory + `FakeTimeProvider` per controllare l'orologio.

- **`CalcolaProssimoToken_usa_ore_da_options_e_clock_corrente`**: scadenza = now + ScadenzaOreDefault.
- **`ValidaTokenAsync_token_inesistente_lancia_NonTrovato`**.
- **`ValidaTokenAsync_token_gia_usato_lancia_GiaUsato`**.
- **`ValidaTokenAsync_token_scaduto_lancia_Scaduto`** (con `FakeTimeProvider` settato dopo la scadenza).
- **`ValidaTokenAsync_token_valido_ritorna_entity`**.

## 8.3 Risultato

Totale test al 2026-05-14: **40 verdi** (31 pre-B.11 + 9 nuovi). Il `dotnet test` impiega ~1 secondo nel run incrementale.

---

# 8bis. Bug fixato in-flight: `MaximumReceiveMessageSize`

Durante il **test live end-to-end** il bottone "Conferma firma" sulla pagina `/firma-impresa/{token}` rimaneva appeso: nessun errore visibile lato browser, nessuna eccezione lato server. I log diagnostici hanno mostrato che `_module.InvokeAsync<string?>("getDataUrl", ...)` partiva ma non tornava mai.

Causa: il default di **SignalR** per `HubOptions.MaximumReceiveMessageSize` è **32 KB**. Il PNG base64 generato da `signature_pad` su uno smartphone Android con `devicePixelRatio = 3` produce un canvas a maggiore risoluzione e una stringa base64 facilmente **> 50 KB**. Il messaggio dal client al server veniva droppato senza notificare il chiamante, e la `Task` lato C# restava in attesa indefinitamente (il `JSInteropDefaultCallTimeout` a 2 minuti l'avrebbe alla fine cancellata, ma l'utente clicca di nuovo prima).

Lo stesso bug era latente anche in B.10 (firma CSE) ma non si era manifestato perché il test live B.10 era stato fatto su tablet, con `devicePixelRatio` minore e dimensioni canvas inferiori.

Fix in `Program.cs`:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
    });
```

1 MB è sovradimensionato per le firme (~50–100 KB) ma sicuro anche per payload grandi futuri (upload base64 di foto piccole, payload custom). Da ricordare se un domani aggiungiamo nuove JSInterop call con dati grandi: prima di tutto, controllare `MaximumReceiveMessageSize`.

---

# 9. Procedura di test live

Pre-requisito: app avviata con `dotnet run --project src/ICMVerbali.Web --launch-profile lan` (vedi memoria `test_da_telefono_profilo_lan` per IP/porta correnti).

1. Login come admin sul PC.
2. Apri una bozza valida (anagrafiche complete, esito + meteo valorizzati), step 10.
3. Clicca "Salva e firma" → disegna la firma → "Conferma firma".
4. Verifica: si apre `LinkImpresaDialog` con il messaggio "Verbale N/AAAA firmato".
5. Clicca "Copia link" → Snackbar "Link copiato negli appunti".
6. Incolla il link in un browser separato (es. Chrome anonimo sul telefono).
7. Verifica: la pagina mostra riepilogo verbale + firma CSE renderizzata + signature pad.
8. Disegna la firma impresa → "Conferma firma".
9. Verifica: la pagina mostra "Firma registrata. Grazie.".
10. Sul PC, torna alla home → il verbale appare con stato "Firmato Impresa".
11. Click sul verbale → si apre `/verbali/{id}` con entrambe le firme renderizzate.
12. Clicca "Stampa" → si apre il dialog di stampa del browser (CSS print nasconde i bottoni "Indietro"/"Stampa" e la nav).

Edge cases da verificare:

- **Riapri il link dopo la firma**: deve mostrare "Link già utilizzato".
- **Apri il link da due tab in parallelo**, firma su una, poi prova a firmare anche sull'altra: la seconda deve fallire con "Token già utilizzato" o stato verbale inconsistente.
- **Token scaduto**: modifica manualmente `FirmaToken.ScadenzaUtc` a un valore nel passato e riapri il link → "Link scaduto".

---

# 10. Cosa non è stato fatto (e perché)

- **Notifica automatica all'impresa** (email/SMS al momento della firma CSE): no SMTP config oggi, il CSE condivide il link manualmente. Rimandato a B.12+ se il cliente lo richiede.
- **Rigenerazione token**: se l'impresa perde il link, oggi serve generarlo manualmente in DB. Una UI "Rigenera link impresa" sul verbale `FirmatoCse` è uno sviluppo futuro.
- **PDF stampabile server-side**: la detail view usa `window.print()` + CSS print-friendly. Sufficiente per "stampa su PDF" dal browser. Una generazione server-side con QuestPDF/iText è feature separata.

---

# 11. File toccati

Aggiunti:

- `src/ICMVerbali.Web/Migrations/002_AddFirmaToken.sql`
- `src/ICMVerbali.Web/Entities/FirmaToken.cs`
- `src/ICMVerbali.Web/Storage/FirmaTokenOptions.cs`
- `src/ICMVerbali.Web/Repositories/Interfaces/IFirmaTokenRepository.cs`
- `src/ICMVerbali.Web/Repositories/FirmaTokenRepository.cs`
- `src/ICMVerbali.Web/Managers/Interfaces/IFirmaTokenManager.cs`
- `src/ICMVerbali.Web/Managers/FirmaTokenManager.cs`
- `src/ICMVerbali.Web/Managers/FirmaTokenInvalidoException.cs`
- `src/ICMVerbali.Web/Components/Layout/PublicLayout.razor`
- `src/ICMVerbali.Web/Components/Pages/FirmaImpresa.razor`
- `src/ICMVerbali.Web/Components/Pages/Verbali/VerbaleDetail.razor`
- `src/ICMVerbali.Web/Components/Shared/LinkImpresaDialog.razor`
- `tests/ICMVerbali.Tests/Managers/FirmaTokenManagerTests.cs`
- `docs/B11-firma-impresa.md` (questo file)

Modificati:

- `src/ICMVerbali.Web/Program.cs` (DI di token repo/manager + FirmaTokenOptions)
- `src/ICMVerbali.Web/appsettings.json` (sezione `FirmaToken`)
- `src/ICMVerbali.Web/Repositories/Interfaces/IVerbaleRepository.cs` (signature FirmaCseAsync estesa + nuovo FirmaImpresaAsync + record FirmaTokenInputs + FirmaCseResult.TokenImpresa)
- `src/ICMVerbali.Web/Repositories/VerbaleRepository.cs` (INSERT token + implementazione FirmaImpresaAsync)
- `src/ICMVerbali.Web/Managers/Interfaces/IVerbaleManager.cs` (signature aggiornata + FirmaImpresaAsync)
- `src/ICMVerbali.Web/Managers/VerbaleManager.cs` (DI IFirmaTokenManager + implementazione FirmaImpresaAsync)
- `src/ICMVerbali.Web/Components/Pages/Verbali/WizardStep10Riepilogo.razor` (apre LinkImpresaDialog dopo firma)
- `src/ICMVerbali.Web/Components/Pages/Home.razor` (verbali non-bozza aprono detail view)
- `docs/01-design.md` (Addendum 2026-05-14)
- `tests/ICMVerbali.Tests/Managers/VerbaleManagerPrescrizioniTests.cs` (mock aggiornato)
- `tests/ICMVerbali.Tests/Repositories/VerbaleRepositoryTests.cs` (helper BuildTokenInputs + 4 nuovi test)
