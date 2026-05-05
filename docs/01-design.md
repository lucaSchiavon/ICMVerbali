# 01 — Design di ICMVerbali

> Documento di design redatto sulla base del modulo cartaceo `Verbale_sicurezza.pdf`
> e dei requisiti raccolti in apertura sessione. **Non contiene codice**: serve a
> congelare le scelte prima della Fase B (implementazione).
>
> Stato: **DRAFT — in attesa di approvazione voce per voce della Sezione 9.**

---

## 1. Requisiti funzionali (sintesi)

Reso esplicito quanto fornito + integrazioni dedotte dal PDF.

| # | Requisito | Note |
|---|---|---|
| RF-01 | Login utente (cookie auth, no SSO esterno) | Conferma da PDF: il verbale è firmato da personale ICM Solutions (il CSE). |
| RF-02 | Homepage post-login = lista verbali della giornata corrente | Default filtro `data = today`, con possibilità di cambiarne periodo. |
| RF-03 | Pulsante "+" per nuovo verbale | Apre il wizard sezione 1 in modalità bozza. |
| RF-04 | Edit di verbale esistente | Rispettando lo stato (vedi RF-09). |
| RF-05 | Upload foto con didascalia (sez. 9 PDF) | Numero foto non fissato dal PDF (ne mostra 6, ma l'app deve consentire N). |
| RF-06 | Responsive desktop / tablet / smartphone | Uso primario in cantiere → mobile-first. |
| RF-07 | Salvataggio bozza incrementale | Necessario per uso in cantiere su rete instabile (vedi §10). |
| RF-08 | Esito verifica complessiva | Enum 4 valori, vedi §2. |
| RF-09 | Workflow firme: CSE prima, Impresa entro 24h | Dedotto da PDF p.4 ("Validità e termini") + p.5 (due date di firma diverse: CSE 16/04, Impresa 28/04). |
| RF-10 | Numero verbale univoco | Nel PDF "VERBALE N. 2" → numerazione progressiva. **Strategia di numerazione nei punti aperti (§9.10)**. |

Non desunti dal PDF e quindi NON inclusi qui (vedi §9):
- Esportazione PDF stilizzato del verbale firmato.
- Notifiche all'Impresa al completamento del verbale.
- Audit / cronologia modifiche.
- Ruoli utente diversi dal "compilatore CSE".

---

## 2. Modello di dominio

### 2.1 Entità identificate

Attribuisco a ogni entità: tipo (**E**ntità con identità persistente, **VO** value object, **C** catalogo), e una giustificazione legata al PDF.

#### `Verbale` — **E** (aggregate root)
Il documento centrale. Una riga per ogni sopralluogo.

| Proprietà | Tipo C# | Note |
|---|---|---|
| `Id` | `Guid` | PK. |
| `Numero` | `int` | "VERBALE N. 2" sul PDF. Vedi §9.10 per la strategia di assegnazione. |
| `Data` | `DateOnly` | Data del sopralluogo (separata dalle date di firma). |
| `CantiereId` | `Guid` | FK → `Cantiere`. |
| `CommittenteId` | `Guid` | FK → `Committente`. |
| `ImpresaAppaltatriceId` | `Guid` | FK → `ImpresaAppaltatrice`. |
| `RuoloLavoriPersonaId` | `Guid` | FK → `Persona` (RL). |
| `CsPersonaId` | `Guid` | FK → `Persona` (CSP). |
| `CsePersonaId` | `Guid` | FK → `Persona` (CSE). |
| `DlPersonaId` | `Guid` | FK → `Persona` (DL). |
| `EsitoVerifica` | `EsitoVerifica` (enum) | Conforme / NC minori / NC gravi / Sospensione. |
| `CondizioneMeteo` | `CondizioneMeteo` (enum) | Sereno / Nuvoloso / Pioggia / Neve. |
| `TemperaturaCelsius` | `int?` | Sul PDF è un campo intero ("23"). Nullable se non rilevata. |
| `GestioneInterferenze` | `GestioneInterferenze` (enum) | Nessuna / Interne / Con aree esterne. |
| `GestioneInterferenzeNote` | `string?` | Free text (sez. 7). |
| `Stato` | `StatoVerbale` (enum) | Bozza / FirmatoCSE / FirmatoImpresa / Chiuso. **Sostituisce il flag `IsDraft`** (vedi §3.4). |
| `CompilatoDaUtenteId` | `Guid` | FK → `Utente`. |
| `CreatedAt` | `DateTime` | UTC. |
| `UpdatedAt` | `DateTime` | UTC, aggiornato a ogni save. |

#### `Cantiere` — **E** (riusabile su più verbali)
Sul PDF: "Pegognaga (MN), Via Trentin / Nuova Costruzione di due magazzini WHA e WHD / € 24.239.887,54". Lo stesso cantiere può essere oggetto di sopralluoghi settimanali per mesi → **estrazione obbligatoria**, non incorporato nel `Verbale`.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `Ubicazione` | `string` |
| `Tipologia` | `string` |
| `ImportoAppalto` | `decimal?` |
| `IsAttivo` | `bool` |

#### `Committente` — **E** (riusabile)
Sul PDF: "Investire SGR S.p.A. Fondo Metis ...". Lo stesso committente può avere più cantieri.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `RagioneSociale` | `string` |
| `Indirizzo` | `string?` |
| `CodiceFiscale` | `string?` |
| `PartitaIva` | `string?` |
| `NumeroIscrizioneRegistroImprese` | `string?` |

#### `ImpresaAppaltatrice` — **E** (riusabile)
Stessi campi di `Committente`. Decisione di design: **due tabelle distinte invece di una `Azienda` unica**. Motivo: i due ruoli sono semanticamente diversi (committente = chi paga, impresa = chi esegue), un'azienda non è in pratica entrambi nello stesso cantiere e separare riduce ambiguità nei filtri/dropdown. **Trade-off** discusso in §9.13.

#### `Persona` — **E** (riusabile)
Per le 4 figure di legge (RL/CSP/CSE/DL) e per le presenze al sopralluogo (sez. 1).
Sul PDF: "Ing. Stefano Barbi - ICM Solutions", "Arch. Clara Cordioli - ICM Solutions", "Ing. Paolo Fraccaroli ICM Solutions Srl", ecc. Le stesse persone ricorrono in più verbali → estrazione obbligatoria.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `Nominativo` | `string` (es. "Ing. Stefano Barbi") |
| `Azienda` | `string` (es. "ICM Solutions") — testo libero, non FK, perché può non corrispondere a `Committente`/`ImpresaAppaltatrice` |
| `IsAttivo` | `bool` |

> **Nota**: NON normalizzo `Azienda` di `Persona` come FK perché nel PDF compaiono aziende ("ICM Solutions", "Crosslog Srl") che non sono né committenti né impresa appaltatrice del verbale. Tenerlo come testo libero è più semplice e rispecchia il modulo cartaceo.

#### `Presenza` — **E** dipendente da `Verbale` (entità di relazione)
Sezione 1 del PDF, fino a 8 righe sul modulo, ma N nell'app.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `VerbaleId` | `Guid` (FK) |
| `PersonaId` | `Guid?` (FK opzionale: chi non è in anagrafica si scrive a mano) |
| `NominativoLibero` | `string?` (usato se `PersonaId` null) |
| `ImpresaLibera` | `string?` (idem) |
| `Ordine` | `int` |

> **Decisione**: la presenza ammette sia `PersonaId` (autocomplete dall'anagrafica) sia testo libero (se è una persona "una tantum"). Cattura il comportamento reale del cantiere senza forzare a creare anagrafiche per ogni transito.

#### Cataloghi — **C**
Quattro cataloghi paralleli per le sezioni 3, 4, 5, 6 del PDF. Tutti seguono lo stesso pattern (`Id`, `Codice` immutabile, `Etichetta`, `Ordine`, `IsAttivo`), più eventuali campi propri.

- `CatalogoTipoAttivita` (sez. 3): 16 voci fisse: `Allestimento/Smobilizzo`, `Demolizioni/Rimozioni`, `Scavi/Movimenti terra`, `Fondazioni/Opere C.A.`, `Strutture Prefabbricate`, `Carpenteria Metallica`, `Tamponature/Murature`, `Coperture/Impermeabilizzazioni`, `Serramenti e Infissi`, `Impianti Elettrici`, `Impianti Meccanici/Idraulici`, `Pavimentazioni`, `Tinteggiature`, `Finiture/Cartongessi`, `Opere Esterne/Verde`, `Altro` (con free text).
- `CatalogoTipoDocumento` (sez. 4): `Notifica Preliminare`, `Libretti Ponteggi / PIMUS`, `Fascicoli Macchine/Attrezzature`, `Altro`.
- `CatalogoTipoApprestamento` (sez. 5): 7 voci raggruppate in 4 sottosezioni:
  - 5.1 Organizzazione: `Recinzione/Cartelli/Viabilità`, `Stoccaggio/Rifiuti/Servizi`
  - 5.2 Cadute dall'alto: `Ponteggi`, `Parapetti/Scale/LineeVita`
  - 5.3 Emergenze & DPI: `Estintori/PrimoSoccorso/VieFuga`, `DPI`
  - 5.4 Impianti: `Impianto Elettrico Cantiere`
  Campo extra: `Sottosezione` (`5.1` / `5.2` / `5.3` / `5.4`).
- `CatalogoTipoCondizioneAmbientale` (sez. 6): `Illuminazione`, `Polveri`, `Rumore`, `PuliziaStrade`.

**Politica di immutabilità**: ai cataloghi si **non si fa hard delete**, solo `IsAttivo = false`. Il `Codice` (es. `ATTIVITA_SCAVI_MOVIMENTI_TERRA`) è immutabile per non rompere i riferimenti storici. L'`Etichetta` può essere ritoccata per typo, ma cambi semantici devono essere fatti creando una nuova voce e disattivando la vecchia.

#### Tabelle di relazione `Verbale ↔ Catalogo` — **E** dipendenti
Sono 4 tabelle parallele, ognuna con il proprio schema (vedi §3.3 per la motivazione del rifiuto della tabella parametrica unica):

- `VerbaleAttivita`: `(VerbaleId, CatalogoTipoAttivitaId, Selezionato bool, AltroDescrizione string?)`.
- `VerbaleDocumento`: `(VerbaleId, CatalogoTipoDocumentoId, Applicabile bool, Conforme bool, Note string?, AltroDescrizione string?)`.
- `VerbaleApprestamento`: `(VerbaleId, CatalogoTipoApprestamentoId, Applicabile bool, Conforme bool, Note string?)`.
- `VerbaleCondizioneAmbientale`: `(VerbaleId, CatalogoTipoCondizioneAmbientaleId, Conforme bool, NonConforme bool, Note string?)`.

> **Nota sulla sez. 6**: il PDF ha colonne `CONF.` e `NC` invece di `APPL.` e `CONF.` come nelle altre. Le due colonne **non sono mutuamente esclusive nel layout** (entrambe sono caselle di check), ma semanticamente lo sono. Modello con due bool distinti ma applico vincolo logico nel Manager (`Conforme XOR NonConforme`).

#### `PrescrizioneCSE` — **E** dipendente
Sez. 8: free text con N osservazioni del CSE. Sul PDF appaiono come righe distinte.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `VerbaleId` | `Guid` (FK) |
| `Testo` | `string` |
| `Ordine` | `int` |

> **Decisione**: lista di item (non singolo blob testo) per consentire futuro tracking accettazione/contestazione 24h prevista dalla nota legale di p.4. Non implementiamo il workflow ora ma il modello dati lo supporta. Vedi §9.16.

#### `Foto` — **E** dipendente
Sez. 9.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `VerbaleId` | `Guid` (FK) |
| `FilePathRelativo` | `string` |
| `Didascalia` | `string?` |
| `Ordine` | `int` |
| `CreatedAt` | `DateTime` |

#### `Firma` — **E** dipendente
Pagina 5 del PDF.

| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `VerbaleId` | `Guid` (FK) |
| `Tipo` | `TipoFirmatario` (enum: `Cse` / `ImpresaAppaltatrice`) |
| `NomeFirmatario` | `string` (es. "Arch. Clara Cordioli", "Dott. Alessandro Lonardi") |
| `DataFirma` | `DateOnly` |
| `ImmagineFirmaPath` | `string?` (vedi §9.8 per signature pad) |

#### `Utente` — **E**
| Proprietà | Tipo |
|---|---|
| `Id` | `Guid` |
| `Username` | `string` |
| `Email` | `string?` |
| `PasswordHash` | `string` |
| `Ruolo` | `RuoloUtente` (enum) — **schema da decidere in §9.4 / §9.14** |
| `IsAttivo` | `bool` |
| `CreatedAt`, `UpdatedAt` | `DateTime` |

### 2.2 Enum identificati

```
EsitoVerifica       : Conforme | NcMinori | NcGravi | Sospensione
CondizioneMeteo     : Sereno | Nuvoloso | Pioggia | Neve
GestioneInterferenze: Nessuna | InterneAlCantiere | ConAreeEsterne
StatoVerbale        : Bozza | FirmatoCse | FirmatoImpresa | Chiuso
TipoFirmatario      : Cse | ImpresaAppaltatrice
SottosezioneApprest : S5_1 | S5_2 | S5_3 | S5_4
RuoloUtente         : (da decidere — vedi §9.14)
```

> **Persistenza enum in DB**: come `tinyint` (per `EsitoVerifica`, `CondizioneMeteo`, `Stato`, ecc.) o come `varchar(40)` (per leggibilità in query ad-hoc). **Raccomandazione**: `tinyint` con vincolo `CHECK (col IN (0,1,2,3))`, più leggibile e veloce, ma documentare la mappatura in commento sulla tabella. Decisione minore, non in §9.

### 2.3 Diagramma testuale delle relazioni

```
            Utente
              │
              │ 1..*  (compilato_da)
              ▼
   ┌──────────────────┐                      ┌─────────────────┐
   │     Verbale      │ *  ─────────  1      │    Cantiere     │
   │  (aggregate root)│ ─────────────────►   └─────────────────┘
   └─────────┬────────┘
             │   * ──── 1   ┌───────────────┐
             ├─────────────►│  Committente  │
             │              └───────────────┘
             │   * ──── 1   ┌──────────────────────┐
             ├─────────────►│ ImpresaAppaltatrice  │
             │              └──────────────────────┘
             │   * ──── 1   ┌──────────┐
             ├─────────────►│ Persona  │ (×4 FK: RL, CSP, CSE, DL)
             │              └──────────┘
             │
             │ 1
             ▼
   ┌─────────────────────────────────────────────────────────────┐
   │  Aggregati figli (cascade-delete, una sola FK verso Verbale)│
   ├─────────────────────────────────────────────────────────────┤
   │ Presenza            *──► Persona? (FK opzionale)            │
   │ VerbaleAttivita     *──► CatalogoTipoAttivita               │
   │ VerbaleDocumento    *──► CatalogoTipoDocumento              │
   │ VerbaleApprestamento*──► CatalogoTipoApprestamento          │
   │ VerbaleCondAmbient. *──► CatalogoTipoCondizioneAmbientale   │
   │ PrescrizioneCSE                                              │
   │ Foto                                                          │
   │ Firma                                                         │
   └─────────────────────────────────────────────────────────────┘
```

### 2.4 Note di aggregato
- L'aggregate root è `Verbale`. Tutte le entità "figlie" (Presenza, VerbaleXxx, PrescrizioneCSE, Foto, Firma) hanno una sola FK verso `Verbale` e devono essere `cascade delete` se permettiamo hard delete (vedi §9.6).
- `Cantiere`, `Committente`, `ImpresaAppaltatrice`, `Persona`, `Utente`, `Catalogo*` sono **aggregati indipendenti**: vivono di vita propria e NON si cancellano insieme al verbale.

---

## 3. Schema database (prima migration)

> **Solo struttura tabellare**. Lo script SQL completo lo scriviamo in Fase B come `001_InitialSchema.sql`. Convenzioni: PK `uniqueidentifier` (Guid), enum come `tinyint`, timestamp come `datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()`.

### 3.1 Tabelle anagrafiche

| Tabella | PK | Colonne principali (con tipi SQL Server) | Indici |
|---|---|---|---|
| `Utente` | `Id uniqueidentifier` | `Username nvarchar(80) NOT NULL`, `Email nvarchar(200) NULL`, `PasswordHash nvarchar(200) NOT NULL`, `Ruolo tinyint NOT NULL`, `IsAttivo bit NOT NULL`, `CreatedAt`, `UpdatedAt` | UNIQUE su `Username`, UNIQUE filtrato su `Email` (`WHERE Email IS NOT NULL`) |
| `Cantiere` | `Id uniqueidentifier` | `Ubicazione nvarchar(300) NOT NULL`, `Tipologia nvarchar(500) NOT NULL`, `ImportoAppalto decimal(18,2) NULL`, `IsAttivo bit NOT NULL` | INDEX su `IsAttivo` |
| `Committente` | `Id uniqueidentifier` | `RagioneSociale nvarchar(250) NOT NULL`, `Indirizzo nvarchar(400) NULL`, `CodiceFiscale nvarchar(20) NULL`, `PartitaIva nvarchar(20) NULL`, `NumeroIscrizioneRegistroImprese nvarchar(80) NULL`, `IsAttivo bit NOT NULL` | INDEX su `RagioneSociale` |
| `ImpresaAppaltatrice` | `Id uniqueidentifier` | (stessi campi di `Committente`) | INDEX su `RagioneSociale` |
| `Persona` | `Id uniqueidentifier` | `Nominativo nvarchar(200) NOT NULL`, `Azienda nvarchar(200) NULL`, `IsAttivo bit NOT NULL` | INDEX su `Nominativo` |

### 3.2 Cataloghi (4 tabelle parallele)

Pattern comune a tutti e 4:

| Colonna | Tipo SQL |
|---|---|
| `Id` | `uniqueidentifier` PK |
| `Codice` | `varchar(80) NOT NULL UNIQUE` (immutabile) |
| `Etichetta` | `nvarchar(200) NOT NULL` |
| `Ordine` | `int NOT NULL` |
| `IsAttivo` | `bit NOT NULL` |

Differenze:
- `CatalogoTipoApprestamento` aggiunge `Sottosezione tinyint NOT NULL` (1=5.1 ... 4=5.4).

I cataloghi sono **seedati dalla prima migration** con i valori del PDF.

### 3.3 Tabelle delle sezioni del verbale: separate vs parametrica

**Opzione A — 4 tabelle separate** (raccomandata):

| Tabella | Colonne |
|---|---|
| `VerbaleAttivita` | `(VerbaleId, CatalogoTipoAttivitaId, Selezionato bit, AltroDescrizione nvarchar(300) null)` PK composta |
| `VerbaleDocumento` | `(VerbaleId, CatalogoTipoDocumentoId, Applicabile bit, Conforme bit, Note nvarchar(500), AltroDescrizione nvarchar(300))` |
| `VerbaleApprestamento` | `(VerbaleId, CatalogoTipoApprestamentoId, Applicabile bit, Conforme bit, Note nvarchar(500))` |
| `VerbaleCondizioneAmbientale` | `(VerbaleId, CatalogoTipoCondizioneAmbientaleId, Conforme bit, NonConforme bit, Note nvarchar(500))` |

**Opzione B — Tabella parametrica unica** `VerbaleCheck`:

```
VerbaleCheck (
  VerbaleId,
  Sezione tinyint,          -- 3=Attività, 4=Documento, 5=Apprestamento, 6=CondAmb
  CatalogoVoceId,           -- FK polimorfico (problema)
  Bool1, Bool2,             -- significato dipende da Sezione
  Note,
  AltroDescrizione
)
```

| Aspetto | Opz. A (separate) | Opz. B (parametrica) |
|---|---|---|
| Aderenza al PDF | Alta — ogni tabella riflette lo schema della sua sezione | Bassa — i 3 schemi diversi (sez.3, 4-5, 6) sono forzati in uno solo |
| FK polimorfica | No, FK normali | Sì — `CatalogoVoceId` non può essere FK SQL (4 tabelle catalogo) |
| Vincoli a livello DB | Possibili (`CHECK (Conforme XOR NonConforme)`) | Difficili (significato di `Bool1/Bool2` dipende dalla sezione) |
| Manager + Repository | 4 metodi `Get*Async` semantici | 1 metodo generico + switch sulle sezioni |
| Cambi futuri (nuovo campo per una sola sezione) | Si aggiunge solo alla tabella interessata | Si aggiunge a tutti, polluzione |
| Performance query | INNER JOIN diretti, indici naturali | Filtro `WHERE Sezione = X` su tabella più grande |
| Volume tabelle | 4 tabelle, ~7+16+4+4 ≈ 31 record per verbale spalmati | 1 tabella, 31 record per verbale concentrati |

**Raccomandazione: Opzione A** (4 tabelle separate). Le 3 sezioni hanno schemi semanticamente diversi (selezione singola vs APPL+CONF vs CONF+NC); forzarli in una tabella unica fa perdere vincoli a livello DB e introduce una FK polimorfica che Dapper non sa gestire nativamente. Il "costo" delle 4 tabelle è basso (catalogo immutabile, schema noto).

### 3.4 Tabella `Verbale`: stato vs flag

Il prompt iniziale propone `IsDraft bit`. Dal PDF emergono però **almeno 4 stati distinti**:
1. Bozza (in compilazione)
2. Firmato CSE (CSE ha apposto firma)
3. Firmato Impresa (entrambi hanno firmato)
4. Chiuso (passate le 24h, accettato)

**Raccomandazione**: sostituire `IsDraft` con `Stato tinyint NOT NULL` (enum `StatoVerbale`). `IsDraft` resta derivabile come `Stato == 0` se serve in query. Le transizioni di stato sono governate dal `VerbaleManager`.

### 3.5 Tabella `Verbale` (struttura completa)

| Colonna | Tipo |
|---|---|
| `Id` | `uniqueidentifier` PK |
| `Numero` | `int NOT NULL` |
| `Anno` | `int NOT NULL` (per supportare numerazione annuale, vedi §9.10) |
| `Data` | `date NOT NULL` |
| `CantiereId` | `uniqueidentifier NOT NULL` FK |
| `CommittenteId` | `uniqueidentifier NOT NULL` FK |
| `ImpresaAppaltatriceId` | `uniqueidentifier NOT NULL` FK |
| `RuoloLavoriPersonaId`, `CsPersonaId`, `CsePersonaId`, `DlPersonaId` | `uniqueidentifier NOT NULL` FK ×4 |
| `EsitoVerifica` | `tinyint NOT NULL` |
| `CondizioneMeteo` | `tinyint NOT NULL` |
| `TemperaturaCelsius` | `int NULL` |
| `GestioneInterferenze` | `tinyint NOT NULL` |
| `GestioneInterferenzeNote` | `nvarchar(1000) NULL` |
| `Stato` | `tinyint NOT NULL` |
| `CompilatoDaUtenteId` | `uniqueidentifier NOT NULL` FK |
| `CreatedAt`, `UpdatedAt` | `datetime2(3) NOT NULL` |

Indici e vincoli:
- UNIQUE su `(Anno, Numero)` per evitare duplicati di numerazione.
- INDEX su `Data` (filtro principale homepage).
- INDEX su `Stato` filtrato (`WHERE Stato = 0`) per query "verbali in bozza".
- INDEX su `CantiereId`.

### 3.6 Tabelle delle entità figlie

| Tabella | Colonne principali |
|---|---|
| `Presenza` | `Id`, `VerbaleId` (FK CASCADE), `PersonaId` FK NULL, `NominativoLibero nvarchar(200) NULL`, `ImpresaLibera nvarchar(200) NULL`, `Ordine int`. CHECK: `PersonaId IS NOT NULL OR NominativoLibero IS NOT NULL`. |
| `PrescrizioneCSE` | `Id`, `VerbaleId` (FK CASCADE), `Testo nvarchar(2000) NOT NULL`, `Ordine int` |
| `Foto` | `Id`, `VerbaleId` (FK CASCADE), `FilePathRelativo nvarchar(500)`, `Didascalia nvarchar(500) NULL`, `Ordine int`, `CreatedAt` |
| `Firma` | `Id`, `VerbaleId` (FK CASCADE), `Tipo tinyint`, `NomeFirmatario nvarchar(200)`, `DataFirma date`, `ImmagineFirmaPath nvarchar(500) NULL`. UNIQUE su `(VerbaleId, Tipo)` |

Le `VerbaleAttivita`/`VerbaleDocumento`/`VerbaleApprestamento`/`VerbaleCondizioneAmbientale` hanno PK composta `(VerbaleId, CatalogoXxxId)` e cascade su `VerbaleId`.

### 3.7 Migration preview (struttura)

Tabelle nella prima migration `001_InitialSchema.sql`:
1. `Utente`
2. `Cantiere`, `Committente`, `ImpresaAppaltatrice`, `Persona`
3. I 4 cataloghi (con seed dei dati dal PDF)
4. `Verbale`
5. `Presenza`, `PrescrizioneCSE`, `Foto`, `Firma`
6. Le 4 tabelle `VerbaleXxx`

Totale: ~14 tabelle. La prima migration include anche il seed dei cataloghi. **Niente seed dell'utente admin in questa migration** (vedi §9.4 per la decisione).

---

## 4. Strategia autenticazione

### 4.1 Tabella `Utente`

Già descritta in §2.1 / §3.1. Campi essenziali: `Username`, `Email` (opzionale, per recupero password), `PasswordHash`, `Ruolo`, `IsAttivo`.

### 4.2 Hashing password

| Algoritmo | Pro | Contro | Disponibilità |
|---|---|---|---|
| **PBKDF2** (via `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>`) | Built-in in `Microsoft.AspNetCore.Identity.Core`, già parte del framework reference se aggiungiamo Identity Core, attivamente mantenuto, formato standard | Più lento di Argon2 a parità di sicurezza, parametri di iterazioni datati nel default | NUGET aggiuntivo: `Microsoft.AspNetCore.Identity` (parte del framework, no install) |
| **BCrypt** (`BCrypt.Net-Next`) | Battle-tested, semplice da usare, salt automatico, output autoesplicativo | Limite hard a 72 bytes di password, NuGet di terze parti | NUGET: `BCrypt.Net-Next` (~ 4.0.x) |
| **Argon2** (`Konscious.Security.Cryptography.Argon2`) | Vincitore PHC 2015, resistente a GPU/ASIC, parametrizzabile per CPU+memoria | Più giovane in .NET, libreria meno diffusa, più tuning richiesto | NUGET: `Konscious.Security.Cryptography.Argon2` |

**Raccomandazione: PBKDF2 via `PasswordHasher<TUser>`.** Motivazioni:
- È built-in nel framework ASP.NET Core, **zero NuGet di terze parti** — coerente con il vincolo "ogni NuGet richiede approvazione".
- L'API è 2 righe: `Hasher.HashPassword(user, "pwd")` e `Hasher.VerifyHashedPassword(user, hash, "pwd")`.
- Per il volume e la sensibilità di un'app aziendale interna è ampiamente sufficiente.
- BCrypt/Argon2 sono giustificati se l'app diventa pubblica con esposizione massiccia. Non è il nostro caso oggi.

> ⚠️ Tuttavia `PasswordHasher<TUser>` è dentro il pacchetto `Microsoft.AspNetCore.Identity` che, sebbene parte del framework reference Microsoft.AspNetCore.App, è un'API specifica di Identity. **Verificare in Fase B se è raggiungibile senza aggiungere `Microsoft.AspNetCore.Identity.EntityFrameworkCore`** (il quale tirerebbe dentro EF Core, non vogliamo). Se richiede un NuGet non incluso nel framework, lo proporrò esplicitamente alla Fase B con conferma.

Conferma in §9.3.

### 4.3 Cookie auth

```
Schema:               CookieAuthenticationDefaults.AuthenticationScheme
LoginPath:            /login
LogoutPath:           /logout
AccessDeniedPath:     /access-denied
ExpireTimeSpan:       8 ore   (turno lavorativo + buffer)
SlidingExpiration:    true
Cookie.Name:          ".ICMVerbali.Auth"
Cookie.HttpOnly:      true
Cookie.SecurePolicy:  Always
Cookie.SameSite:      Lax     (per evitare problemi di redirect post-login)
Cookie.IsEssential:   true    (nessun cookie banner GDPR per cookie tecnico)
```

> Lo schema `Cookie` è incluso in `Microsoft.AspNetCore.Authentication.Cookies` che è parte del framework ASP.NET Core (no NuGet aggiuntivo, già confermato in Fase 3).

### 4.4 Authorization policies

Per ora una sola policy: `RequireAuthenticatedUser`. Quando definiremo i ruoli (§9.14) si aggiungeranno policy come `RequireCseRole`, `RequireAdminRole`. Centralizzate in `Authentication/AuthorizationPolicies.cs` (path da CLAUDE.md).

### 4.5 Seed primo utente admin

Tre opzioni (vedi §9.4):
- **A**: SQL `INSERT` nella prima migration (con hash precalcolato, hard-coded in clear nel migration file).
- **B**: Codice C# in `Program.cs` (o in un `IHostedService` `DatabaseSeeder`) che alla prima esecuzione, se la tabella `Utente` è vuota, crea l'admin con credenziali da `appsettings.json`/secrets/env var.
- **C**: Comando CLI separato (`dotnet run -- seed-admin <username> <password>`).

**Raccomandazione: B**. Il migration file resta declarativo (no segreti versionati), il seeder è idempotente, le credenziali vivono in user-secrets locali e in env var in produzione (coerente con CLAUDE.md "Gestione di segreti e configurazioni"). Decisione formalmente in §9.4.

---

## 5. Elenco pagine Blazor

| Route | Componente | Scopo | Auth |
|---|---|---|---|
| `/login` | `Pages/Login.razor` | Form login | `[AllowAnonymous]` |
| `/logout` | endpoint minimo (handler) | Distrugge cookie | `[Authorize]` |
| `/` | `Pages/Home.razor` | Lista verbali del giorno + pulsante "+" | `[Authorize]` |
| `/verbali` | `Pages/VerbaleList.razor` | Lista verbali con filtri (data, cantiere, stato) | `[Authorize]` |
| `/verbali/nuovo` | `Pages/VerbaleEditor.razor` | Wizard di creazione (avvia bozza) | `[Authorize]` |
| `/verbali/{id:guid}` | `Pages/VerbaleEditor.razor` | Wizard di edit (riprende bozza o ne edita uno firmato in stato modificabile) | `[Authorize]` |
| `/verbali/{id:guid}/visualizza` | `Pages/VerbaleView.razor` | Read-only (dopo firma) | `[Authorize]` |
| `/anagrafica/cantieri` | `Pages/CantiereList.razor` + dialog | CRUD cantieri | `[Authorize]` (futuro: `[Authorize(Policy="Admin")]`) |
| `/anagrafica/committenti` | `Pages/CommittenteList.razor` + dialog | CRUD committenti | come sopra |
| `/anagrafica/imprese` | `Pages/ImpresaList.razor` + dialog | CRUD imprese appaltatrici | come sopra |
| `/anagrafica/persone` | `Pages/PersonaList.razor` + dialog | CRUD persone (figure di legge + presenze ricorrenti) | come sopra |
| `/anagrafica/utenti` | `Pages/UtenteList.razor` + dialog | CRUD utenti | `[Authorize(Policy="Admin")]` quando definito |
| `/access-denied` | `Pages/AccessDenied.razor` | Pagina di errore auth | `[AllowAnonymous]` |

Route dedicate **non** create per ogni step del wizard: il wizard è interno a `VerbaleEditor.razor` e cambia stato (sezione corrente) senza cambiare URL — modifica dell'URL solo se vogliamo rendere lo step deep-linkabile (vedi §9.17).

### 5.1 Componenti riusabili

Identificati dall'osservazione della ripetitività del PDF:

| Componente | Uso | Pattern |
|---|---|---|
| `CheckRowComponent` | Una riga `APPL/CONF/Note` (sez. 4 e 5) | input: bool Applicabile, bool Conforme, string Note, string Etichetta |
| `CondizioneAmbientaleRow` | Una riga `CONF/NC/Note` (sez. 6) — variante del precedente | semantica diversa (vincolo XOR) |
| `AttivitaCheckGrid` | Griglia 16 checkbox (sez. 3) responsiva | mobile: 1 col, tablet: 2 col, desktop: 3 col |
| `EsitoSelector` | Radio 4 valori (header) | radio group |
| `MeteoSelector` | Radio 4 valori + temperatura | radio group + numeric input |
| `PresenzaList` | Lista dinamica add/remove con autocomplete persona | input lista N |
| `PrescrizioneList` | Lista dinamica add/remove di textarea | input lista N |
| `FotoUploader` | Upload + crop/resize client-side opzionale + didascalia | usa input HTML con `capture="environment"` |
| `VerbaleStepper` | Wizard navigabile (laterale desktop, top mobile) | pattern stepper |
| `VerbaleHeaderForm` | Sezione anagrafica iniziale (cantiere/committente/figure) | composto da autocomplete |
| `AnagraficaPicker<T>` | Generic autocomplete + "crea nuovo" inline per cantieri/committenti/imprese/persone | template generic component |

---

## 6. Responsività e struttura del form

### 6.1 Conferme

Tutte le direzioni proposte sono **confermate**. Aggiungo motivi specifici al dominio:

- **Mobile-first**: il PDF è il modulo cartaceo che il CSE oggi compila a mano in cantiere. L'app deve sostituirlo "a quel desk" — su tablet o smartphone con guanti. Desktop è il caso secondario per riepiloghi e revisione.
- **Wizard / stepper**: il PDF stesso è strutturato in 9 sezioni numerate. Pretendere di renderle tutte su una single-page sarebbe ostile in mobile (scroll infinito, validazione opaca). Una sezione = uno step è il mapping naturale.
- **Salvataggio bozza per step**: in cantiere capita di interrompere la compilazione (telefonata, spostamento). Lo stato bozza deve essere persistente entro pochi secondi. Vedi §10.2 per il dettaglio della strategia.
- **`capture="environment"`**: standard HTML supportato da tutti i browser mobili moderni; nel PDF la sezione 9 è la più lunga di tutte. Aprire la fotocamera direttamente è essenziale.
- **Tap target 44×44px**: standard WCAG 2.1 + Apple HIG. **Aumento la richiesta a 48×48px** dove possibile, perché in cantiere si lavora con guanti.
- **Leggibilità outdoor**: contrasto ≥ 7:1 (WCAG AAA, non solo AA) sul testo informativo, perché sotto sole diretto AA non basta.

### 6.2 Direzioni che vorrei contestare/integrare

1. **Stepper desktop laterale**: confermato, ma aggiungo che deve essere sticky (fissato in viewport) e collassabile. In una sezione lunga (es. sez. 5 con 7 apprestamenti) lo stepper non deve scorrere via.
2. **Validazione**: ogni step deve poter essere salvato anche **incompleto** (è il senso di "bozza"), ma il passaggio a `Stato = FirmatoCse` richiede validazione completa di tutti gli step. Quindi due livelli di validazione: "soft" per il salvataggio bozza, "hard" per la firma.
3. **Mobile**: lo stepper top occupa spazio prezioso. **Proposta**: mostrarlo come barra orizzontale di pallini compatta + nome dello step corrente. Tap sul nome → bottom sheet con elenco step.
4. **Step "Riepilogo"** (oltre ai 9 del PDF): l'ultimo step prima della firma è un riepilogo navigabile per facilitare la review. Nel PDF cartaceo questa fase è implicita ("rileggo prima di firmare"); nell'app va resa esplicita.

### 6.3 Anatomia di pagina (pattern generico)

```
┌────────────────────────────────────────────────────────┐
│ AppBar:  [≡] ICMVerbali  [Verbale N. 23 - 16/04/2026]  │
│                                  [bozza]  [esci]       │
├────────────────────────────────────────────────────────┤
│ DESKTOP (≥lg):                                          │
│ ┌──────────────┬────────────────────────────────────┐  │
│ │              │                                    │  │
│ │  Stepper     │   Titolo sezione corrente          │  │
│ │  laterale    │   ─────────────────────────        │  │
│ │  (sticky):   │                                    │  │
│ │              │   [contenuto form]                 │  │
│ │  ✓ 1 Anag.   │                                    │  │
│ │  ✓ 2 Meteo   │                                    │  │
│ │  ▸ 3 Attiv.  │                                    │  │
│ │    4 Doc.    │                                    │  │
│ │    5 Apprst. │   [← Indietro]      [Avanti →]     │  │
│ │    ...       │   [Salva bozza]                    │  │
│ │  9 Foto      │                                    │  │
│ │  ✦ Riepilogo │                                    │  │
│ └──────────────┴────────────────────────────────────┘  │
│                                                         │
│ MOBILE (<md):                                           │
│ ┌────────────────────────────────────────────────────┐ │
│ │ ●●●○○○○○○○  Sez. 3 di 10 ▾                        │ │
│ │ ─────────────────────────────                      │ │
│ │ Titolo sezione corrente                            │ │
│ │                                                    │ │
│ │ [contenuto form scrollabile]                       │ │
│ │                                                    │ │
│ │ ─── footer fisso: ──────                          │ │
│ │ [← Indietro]  [Salva]  [Avanti →]                  │ │
│ └────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
```

### 6.4 Anatomia di sezione di form (pattern ripetuto)

```
┌──── Step header ─────────────────────────────────────┐
│ Sezione N — Titolo                                   │
│ Descrizione breve di cosa va compilato (≤ 1 riga)    │
└──────────────────────────────────────────────────────┘

┌──── Form body ───────────────────────────────────────┐
│                                                      │
│ [campo input principale]                             │
│                                                      │
│ [griglia / tabella checkrow se sezione è lista]      │
│                                                      │
│ [campo Note opzionale]                               │
│                                                      │
│ Validation messages: inline sotto i campi            │
│ Errori bloccanti vs warning informativi              │
└──────────────────────────────────────────────────────┘

┌──── Step footer (sticky) ────────────────────────────┐
│ [auto-save status: "Salvato 12s fa"]                 │
│ [← Indietro]    [Salva bozza]    [Avanti →]          │
└──────────────────────────────────────────────────────┘
```

Tutti gli step seguono questo schema. Le sezioni-lista (3, 4, 5, 6, 9) usano lo stesso pattern + componente lista riusabile.

### 6.5 Accessibilità — implementazione

- Tutti i campi label associate (`<label for>` o `aria-label`).
- Focus order: top-to-bottom, no tabindex hardcoded > 0.
- Errori validazione `aria-live="polite"`.
- Contrasto: tema deve avere foreground/background ≥ 7:1 sul testo principale.
- Niente animazioni essenziali per la comprensione.

---

## 7. Storage immagini

### 7.1 Conferma direzione

**Filesystem locale + path in DB** è la scelta corretta. Argomenti specifici:

- Le foto sono "evidenza di sopralluogo": non vengono modificate dopo l'upload, raramente cancellate, lette poco frequentemente (review post-firma o stampa PDF). Storage da object/blob, non transazionale.
- BLOB SQL gonfia DB e backup, complica restore selettivi e non offre caching browser.
- Filesystem permette CDN/static serving futuro senza migrazione del modello dati.

### 7.2 Schema cartelle

```
{UploadsBasePath}/
  ├── verbali/
  │   └── {verbale-id}/
  │       ├── {foto-id-1}.jpg
  │       ├── {foto-id-2}.jpg
  │       └── ...
  └── firme/
      └── {verbale-id}/
          ├── cse.png
          └── impresa.png
```

`UploadsBasePath` configurato in `appsettings.json` con valore relativo (dev) o assoluto (prod). Mai sotto `wwwroot/` per evitare di esporre tutti i file via static files: il download passa via endpoint controllato (`/api/foto/{id}`) che applica auth + autorizzazione di accesso al verbale.

### 7.3 Servizio dedicato

```
namespace ICMVerbali.Web.Storage;

public interface IFotoStorageService
{
    Task<FotoStorageResult> SalvaAsync(Guid verbaleId, Stream contenuto, string nomeFileOriginale, CancellationToken ct);
    Task<Stream> ApriLetturaAsync(string filePathRelativo, CancellationToken ct);
    Task EliminaAsync(string filePathRelativo, CancellationToken ct);
    Task EliminaTuttoVerbaleAsync(Guid verbaleId, CancellationToken ct);
}
```

Implementazione `LocalFotoStorageService` legge `UploadsBasePath` da `IOptions<StorageOptions>`. Domani un `AzureBlobFotoStorageService` o `S3FotoStorageService` può sostituirla via DI senza toccare `Manager`/`Repository`.

### 7.4 Resize/compressione

Foto da smartphone moderno: 4-8 MB cad., 4000×3000 px. Accettabile per archivio finale: ~ 1920 px lato lungo, JPEG q=85 → ~ 300-500 KB.

Tre librerie candidate (vedi §9.2):
- **SixLabors.ImageSharp** (Six Labors Split License o commercial)
- **SkiaSharp** (MIT, Microsoft)
- **Magick.NET** (Apache-2.0)

Operazioni necessarie:
1. Auto-orient da EXIF (foto smartphone hanno rotazione in metadati, non in pixel).
2. Resize a max 1920 px lato lungo (mantenendo aspect).
3. Re-encode JPEG quality 85, strip EXIF.
4. Generazione thumbnail 320 px per la lista.

Strip EXIF rilevante: foto cantiere possono contenere coordinate GPS che NON vogliamo distribuire (privacy) ma che potremmo voler **salvare separatamente** in `Foto.Latitudine`/`Longitudine` (vedi §9.20).

---

## 8. Design system e component library

### 8.1 Valutazione MudBlazor 9.4.0

**Versione installabile su .NET 10**: ho ispezionato il `nuspec` di MudBlazor 9.4.0 (l'ultima stabile pubblicata su NuGet al 2026-05-05) e il manifesto include esplicitamente `<group targetFramework="net10.0">`. **Compatibilità confermata**.

Verifica esplicita:
```
$ curl -s https://api.nuget.org/v3-flatcontainer/mudblazor/9.4.0/mudblazor.nuspec | grep targetFramework
  <group targetFramework="net8.0">
  <group targetFramework="net9.0">
  <group targetFramework="net10.0">
```

**Compatibilità con InteractiveServer**: la documentazione ufficiale MudBlazor (https://mudblazor.com) indica come render mode predefinito proprio InteractiveServer. Il setup standard prevede:
- Aggiungere `services.AddMudServices()` in `Program.cs`.
- Importare `<MudThemeProvider />`, `<MudPopoverProvider />`, `<MudDialogProvider />`, `<MudSnackbarProvider />` nel `MainLayout.razor`.
- Applicare `@rendermode InteractiveServer` ai componenti che usano interattività (default per MudBlazor v8+).
- Importare CSS/JS via `_Host` o `App.razor` (con `MapStaticAssets()` di .NET 9+ funziona out of the box).

**Nessun problema noto rilevato per InteractiveServer + MudBlazor 9.x.** Casi storicamente segnalati (es. MudDialog con prerendering) sono mitigati settando `prerender:false` sul render mode, **che faremo di default** sui componenti che aprono dialog complessi.

### 8.2 Argomenti pro MudBlazor (dominio specifico)

| Componente MudBlazor | Uso nel nostro form |
|---|---|
| `MudStepper` | Wizard delle 9+1 sezioni |
| `MudFileUpload` | Sez. 9 (foto), gestisce `capture="environment"` |
| `MudCheckBox` | ~ 30 checkbox del PDF (sez. 3, 4, 5, 6) |
| `MudRadioGroup` | Esito (4), Meteo (4), Interferenze (3) |
| `MudTable` / `MudDataGrid` | Lista verbali + righe APPL/CONF/Note |
| `MudDatePicker` | Data verbale + date firme |
| `MudAutocomplete` | Picker cantiere/committente/impresa/persona |
| `MudForm` + `MudTextField`/`MudNumericField` | Tutti i campi testo + temperatura |
| `MudDialog` | Conferme, "crea nuovo" inline anagrafica |
| `MudSnackbar` | Toast "salvato", "errore connessione" |
| `MudGrid` | Layout responsive 1/2/3 colonne |

**Senza MudBlazor**, dovremmo costruire ognuno di questi a mano (Bootstrap puro non offre stepper, autocomplete, datepicker — sarebbero NuGet diversi o JS interop). Stima conservativa: 4-6 settimane di lavoro su componenti riusabili che MudBlazor risolve in 1 giorno di setup.

### 8.3 Argomenti contro MudBlazor da considerare

- **Look "Material"** standard può sembrare poco brand-aligned per ICM Solutions (logo blu navy, layout corporate). Mitigazione: customizzazione tema completa lato C# (`MudThemeProvider`) — colori, raggio bordi, ombre, density.
- **Bundle size**: ~ 600 KB CSS+JS. Per uso in cantiere su 4G/3G potrebbe pesare al primo caricamento. Mitigazione: caching aggressivo + service worker (futuro PWA, vedi §9.19).
- **Lock-in**: cambiare component library più avanti significa riscrivere tutta la UI. Mitigazione: tutti i `Mud*` vengono incapsulati nei nostri componenti riusabili (es. `CheckRowComponent` wrappa `MudCheckBox` ma ne nasconde l'API).
- **Material Design ≠ Cantiere**: Material spinge animazioni e ombre eleganti, ma lo use case "fuori sotto sole con guanti" preferirebbe alta densità di info, niente animazioni superflue. Mitigazione: tema dense + `Variant.Outlined` di default.

### 8.4 Alternative valutate

| Alternativa | Vantaggio | Perché è peggiore di MudBlazor *qui* |
|---|---|---|
| Bootstrap puro (default Blazor) | Zero NuGet aggiuntivo, mainstream | Niente stepper, niente autocomplete: tutto da costruire o tirare dentro 3-4 librerie JS |
| Radzen Blazor | Ottimo DataGrid, free/MIT | Free per uso commerciale ma alcuni componenti richiedono licenza Radzen Studio; ecosistema più piccolo |
| Fluent UI Blazor | Ufficiale Microsoft, stile Office | Set componenti più stretto, MudBlazor offre più copertura form/wizard |
| Tailwind + headless | Massimo controllo design | Costo enorme: niente componenti pronti, tutto custom, ostile per un'app form-heavy |

### 8.5 Tema (se MudBlazor approvato)

```
File:    src/ICMVerbali.Web/Theme/AppTheme.cs
Tipo:    public static class AppTheme { public static MudTheme Default { get; } = new(...) }
```

Definizione centralizzata in C#, **niente SCSS, niente file CSS sparsi nei componenti**. Un solo `wwwroot/css/app.css` per override globali eccezionali (es. fix specifici WebKit mobile). Gli stili dei singoli componenti, se necessari, in file `.razor.css` co-locati (CSS isolation Blazor) — non è violazione del "niente CSS sparso" perché è la pratica standard Blazor e mantiene gli stili nello scope del componente.

Palette: blu navy primario (vicino al logo ICM, codici esatti TBD non in questo doc come da istruzioni); warning/danger/success da default Material. Tipografia: Roboto default. Spacing: default MudBlazor.

Tema chiaro **e** tema "alto contrasto" outdoor: `MudThemeProvider` permette switch via `IsDarkMode`/tema custom. Predispongo l'infrastruttura, decisione se attivare in §9.18.

---

## 9. Punti aperti — DECISIONI APPROVATE IL 2026-05-05

> ✅ **Tutte le 22 voci sotto sono state approvate dall'utente in blocco** in data 2026-05-05.
> Ogni voce mantiene la formulazione originale (opzioni / pro-contro / raccomandazione) come traccia delle motivazioni; la **decisione finale è la "Raccomandazione"** di ciascuna voce.
>
> NuGet la cui adozione è conseguenza diretta di queste decisioni e che andranno installati in Fase B.1 con conferma puntuale del comando: **MudBlazor 9.4.0** (§9.1), **SkiaSharp** (§9.2), **QuestPDF** (§9.11), **libreria signature pad** (§9.8 — da scegliere in B.1).

### 9.1 Component library
- **Opzioni**: (a) MudBlazor 9.4.0, (b) Bootstrap puro (default Blazor) + JS picker custom, (c) Radzen Blazor, (d) Fluent UI Blazor.
- **Pro/contro**: vedi §8.
- **Raccomandazione**: **(a) MudBlazor 9.4.0**. NUGET DA APPROVARE.

### 9.2 Libreria resize/compressione immagini
- **Opzioni**:
  - **SixLabors.ImageSharp** v3.x — license: Six Labors Split License (gratis per uso closed-source non-commercial e per "small" companies con revenue < $1M/anno; commerciale altrimenti). API moderna, pure managed C#.
  - **SkiaSharp** v3.x — license: MIT (Microsoft). Bindings su Skia (C++). Performance eccellente, supporto rotazione EXIF integrato.
  - **Magick.NET** v14.x — license: Apache-2.0. Wrapper di ImageMagick. Massima copertura formati (anche TIFF, RAW), pesante.
- **Pro/contro chiave**:
  - ImageSharp: API più C#-idiomatic, ma licenza ambigua per uso aziendale.
  - SkiaSharp: licenza pulita MIT, performance nativa, l'API è meno fluente ma sufficiente.
  - Magick.NET: overkill per JPEG resize, dipendenza nativa enorme (~ 30 MB).
- **Raccomandazione**: **SkiaSharp**. Licenza MIT senza ambiguità (importante in contesto aziendale ICM Solutions), supportata da Microsoft, peso accettabile, copre tutto quello che ci serve (resize, auto-orient EXIF, encode JPEG quality). NUGET DA APPROVARE.

### 9.3 Hashing password
- **Opzioni**: (a) PBKDF2 via `PasswordHasher<TUser>` di ASP.NET Core Identity, (b) BCrypt.Net-Next, (c) Konscious Argon2.
- **Pro/contro**: vedi §4.2.
- **Raccomandazione**: **(a) PBKDF2**. Verificare in Fase B se `PasswordHasher<TUser>` è raggiungibile senza tirare dentro `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Se serve un NuGet, lo proporrò e approverai. Possibile fallback **(b) BCrypt.Net-Next** se PBKDF2 risultasse impraticabile per ragioni di packaging.

### 9.4 Seed primo utente admin
- **Opzioni**: (a) INSERT in migration con hash precalcolato, (b) `IHostedService` `DatabaseSeeder` idempotente che legge credenziali da configurazione/env-var, (c) comando CLI separato.
- **Pro/contro**:
  - (a): semplice ma hash hardcoded e versionato — anti-pattern.
  - (b): credenziali fuori dal repo (user-secrets in dev, env var in prod), idempotente, eseguito al boot.
  - (c): pulito ma richiede passo manuale extra in deploy.
- **Raccomandazione**: **(b)** + setting `Admin:DefaultUsername` / `Admin:DefaultPassword` in `appsettings` con placeholder (la password reale solo in user-secrets/env). Il seeder esegue solo se `Utente` è vuoto.

### 9.5 Multi-tenant vs single-tenant
- **Opzioni**: (a) single-tenant (solo ICM Solutions), (b) multi-tenant (più organizzazioni / più CSE).
- **Cosa cambia**: in (b) tutte le entità anagrafiche (Cantiere, Committente, Impresa, Persona, Utente) avrebbero un `OrganizationId`, le query un filtro implicito (`WHERE OrganizationId = @currentOrg`), policy auth `RequireSameOrg`.
- **Raccomandazione**: **(a) single-tenant ora**. NON c'è nel PDF nessun indizio di multi-tenancy: tutti i CSE/RL/CSP/DL sono "ICM Solutions". Aggiungere multi-tenancy in seguito è doloroso ma fattibile (migration + filtri). Aggiungerla preventivamente è scope creep. **Confermami che ICM Solutions non prevede di rivendere/condividere l'app a controllate.**

### 9.6 Soft vs hard delete
- **Opzioni**: (a) soft delete (`IsDeleted` flag su `Verbale`), (b) hard delete cascade.
- **Pro/contro**:
  - (a): tracciabilità, recupero, ma query devono filtrare ovunque; rischio dimenticanze; il flag finisce su tutte le entità figlie.
  - (b): pulito, ma irreversibile.
- **Raccomandazione**: **(a) soft delete sul `Verbale`**, con cascade *logico* sulle figlie (le figlie restano in DB, marcate "orfane di un verbale soft-deleted" ma non si toccano). Le anagrafiche (Cantiere, Committente, ecc.) NON usano soft delete, usano `IsAttivo`. Motivo: un verbale firmato è un documento legale (D.Lgs. 81/2008): cancellarlo davvero è rischioso. Per le anagrafiche `IsAttivo=false` è sufficiente.

### 9.7 Localizzazione
- **Opzioni**: (a) solo italiano hardcoded, (b) i18n da subito (resource `.resx`), (c) i18n predisposto ma popolato solo IT.
- **Raccomandazione**: **(a) solo italiano**. Il PDF è solo italiano, gli utenti sono italiani, il D.Lgs. 81/2008 è normativa italiana. Predisporre i18n ora è premature optimization. Se in 2-3 anni servirà, è una migration di stringhe gestibile.

### 9.8 Strategia firme finali
- **Opzioni**:
  - (a) campo testo "Firmato da X il GG/MM/AAAA" + checkbox dichiarativa.
  - (b) signature pad inline (canvas HTML) → immagine PNG → file in `firme/`.
  - (c) firma digitale qualificata (CAdES/PAdES con smart card o SPID).
- **Pro/contro**:
  - (a): banale, ma valore legale dubbio.
  - (b): UX da tablet eccellente (corrisponde al gesto cartaceo), valore legale come "firma elettronica semplice", richiede signature pad component.
  - (c): valore legale pieno (firma elettronica qualificata) ma scope significativo (integrazione SPID/CIE/firma remota), tablet/smartphone con lettore smart card non è realistico in cantiere.
- **Raccomandazione**: **(b) signature pad inline**. È coerente con l'UX cartacea attuale ("firma e timbro" in fondo al modulo), funziona con dito/stylus su tablet, richiede solo un component canvas (esiste in MudBlazor extensions o libreria standalone — verificheremo in Fase B). Salviamo PNG + hash della firma per ridurre rischio sostituzione. Per la firma qualificata SPID lasciamo la porta aperta architettonicamente (il campo `Firma.ImmagineFirmaPath` può diventare `Firma.DocumentoFirmatoPath` in futuro).

### 9.9 Workflow accettazione 24h prescrizioni CSE
Il PDF p.4 cita: *"In assenza di osservazioni scritte e motivate da parte dell'Impresa Affidataria entro 24 ore... si intendono ACCETTATE."*
- **Opzioni**: (a) ignorato in app (gestione fuori sistema, come oggi), (b) tracking automatico: ogni `PrescrizioneCSE` ha `StatoPrescrizione` (Pendente/Accettata/Contestata) con data scadenza e auto-transizione, (c) tracking manuale lato Impresa (l'impresa logga e accetta/contesta esplicitamente).
- **Raccomandazione**: **(a) ignorato in v1**. È un workflow significativo che richiede ruoli "Impresa", notifiche, tracking timer, possibili allegati di contestazione → scope creep. Il modello dati è già predisposto (lista di prescrizioni, non blob), quindi v2 è fattibile senza migration distruttive. **Conferma**.

### 9.10 Numerazione verbali
- **Opzioni**: (a) globale crescente (`1, 2, 3, ...`), (b) annuale (`1/2026, 2/2026, ...`), (c) per cantiere (`Cantiere-A/1, Cantiere-A/2, ...`), (d) per cantiere e anno.
- **Raccomandazione**: **(b) annuale globale** con UNIQUE `(Anno, Numero)`. Coerente con le pratiche italiane di "Verbale N. X dell'anno Y", semplice da implementare, ricomincia da 1 ogni 1 gennaio. Numero auto-assegnato dal Manager al passaggio Bozza→FirmatoCSE (NON in creazione, perché un verbale bozza poi cancellato non deve "bruciare" un numero). **Conferma**.

### 9.11 Esportazione PDF
Non chiesto esplicitamente, ma è un'aspettativa naturale: il verbale firmato deve essere stampabile/inviabile come PDF analogo al cartaceo.
- **Opzioni**: (a) niente PDF (solo schermata), (b) generazione PDF lato server (QuestPDF dual-license / PdfSharp+MigraDoc MIT / iText 8 AGPL+commercial), (c) generazione lato browser (`window.print()` → CSS print).
- **License QuestPDF**: contrariamente a quanto inizialmente scritto, NON è MIT puro. È "Community MIT" (gratis per: revenue < $1M USD/anno, non-profit, dipendenza transitiva) oppure Professional/Enterprise a pagamento per aziende > $1M USD revenue. **Da chiarire la posizione di ICM Solutions** prima di adottarla.
- **Raccomandazione aggiornata 2026-05-05**: scelta della libreria PDF **rinviata alla sotto-fase B.9** (export PDF). Le opzioni di lavoro sono: (b1) QuestPDF se ICM rientra nei requisiti Community MIT; (b2) PdfSharp+MigraDoc come fallback MIT puro. Decisione finale al momento dell'implementazione effettiva.

### 9.12 Audit log / cronologia modifiche
Non richiesto, ma per documenti legali è prudente.
- **Opzioni**: (a) niente, (b) tabella `VerbaleAudit` semplice (chi/quando/quale-stato-è-passato), (c) snapshot completi (event sourcing leggero).
- **Raccomandazione**: **(b) audit log minimal**: ogni transizione di stato (`Bozza→FirmatoCSE`, `FirmatoCSE→FirmatoImpresa`, ecc.) genera un record `VerbaleAudit (VerbaleId, DataEvento, UtenteId, EventoTipo, Note)`. Niente diff dei contenuti (troppo pesante). **Conferma**.

### 9.13 Modello aziende: una tabella o due
- **Opzioni**: (a) tabelle separate `Committente` e `ImpresaAppaltatrice`, (b) tabella unica `Azienda` con flag `RuoloPredominante`.
- **Raccomandazione**: **(a) separate** come da §2.1. Domanda: confermi che committente e impresa non si sovrappongono nello stesso cantiere? (Se sì, (a) è chiaramente meglio.)

### 9.14 Ruoli utente
- **Opzioni**: (a) un solo ruolo "Utente" (= CSE, può fare tutto tranne creare altri utenti), (b) due ruoli `Cse` + `Admin` (Admin gestisce utenti/cataloghi), (c) modello più granulare (`Cse`, `Visualizzatore`, `Admin`).
- **Raccomandazione**: **(b) due ruoli**, con policy `RequireAdminRole` solo su `/anagrafica/utenti` e (futuro) gestione cataloghi. Il "compilatore" tipico è `Cse`. Il primo utente seedato è `Admin`.

### 9.15 Visualizzazione/edit di un verbale già firmato
- **Opzioni**: (a) read-only assoluto dopo firma CSE, (b) editabile fino a firma Impresa, poi read-only, (c) sempre editabile con audit.
- **Raccomandazione**: **(b)**. Coerente col valore legale crescente: Bozza = libero edit, FirmatoCSE = read-only per CSE ma l'Impresa può aggiungere note/firma, FirmatoImpresa/Chiuso = read-only per tutti. Modifiche post-firma → nuovo verbale, non edit. **Conferma**.

### 9.16 Granularità sezione 8 (prescrizioni)
- **Opzioni**: (a) lista di item separati (`PrescrizioneCSE` come da §2.1), (b) singolo blob `nvarchar(max)`.
- **Raccomandazione**: **(a) lista**. Già giustificata in §2.1: predispone al workflow §9.9 e all'export PDF strutturato. **Conferma**.

### 9.17 Deep-link dello step del wizard
- **Opzioni**: (a) URL invariato durante il wizard (refresh = torna a step 1), (b) URL cambia con query string `?step=N` (refresh = stesso step), (c) URL cambia con segmento route `/verbali/{id}/step/{n}`.
- **Raccomandazione**: **(b) query string**. Soluzione semplice, supporta back/forward del browser, non rompe la struttura della route principale. **Conferma**.

### 9.18 Tema dark/alto contrasto
- **Opzioni**: (a) solo light, (b) light + dark, (c) light + alto contrasto outdoor.
- **Raccomandazione**: **(c) light + alto contrasto outdoor**. Il dark mode in cantiere sotto sole peggiora la leggibilità, l'alto contrasto la migliora. Switch dall'AppBar.

### 9.19 PWA / funzionamento offline
Non chiesto. Lo cito come open point per evitare regret futuri.
- **Opzioni**: (a) niente PWA, (b) PWA installabile (manifest + service worker base, no offline) — utile per "Aggiungi a Home" su tablet, (c) PWA con offline completo (sincronizzazione bozze in IndexedDB).
- **Raccomandazione**: **(b)**. Setup costo basso, valore percepito alto (icona sul tablet, splash screen). Offline completo (c) è incompatibile con InteractiveServer (richiede WebAssembly o API offline-first) → no scope. **Decisione tua: PWA installabile in v1 o non per ora?**

### 9.20 GPS / metadati foto
- **Opzioni**: (a) ignorare GPS, (b) leggere lat/lon da EXIF e salvarli su `Foto.Latitudine`/`Longitudine` (e mostrare mappina opzionale), (c) GPS attivo via API browser e salvato per l'intero verbale.
- **Raccomandazione**: **(a) ignorare in v1**. Privacy-by-default, niente esposizione coordinate. Stripping EXIF in upload (vedi §7.4) elimina anche il problema. Si può riabilitare in v2 se utile per audit.

### 9.21 Numero massimo di foto per verbale
- Il PDF ne mostra 6, ma in pratica un sopralluogo può produrne 20+.
- **Opzioni**: (a) limite 20, (b) limite 50, (c) nessun limite (con safeguard via singolo file ≤ 10 MB).
- **Raccomandazione**: **(b) limite 50** + singolo file ≤ 10 MB pre-resize. Difensivo contro upload accidentali da galleria. **Conferma**.

### 9.22 Validazione obbligatorietà alla firma
Quali sezioni sono obbligatorie per passare `Bozza → FirmatoCse`?
- Necessariamente: header (Data, Cantiere, Committente, Impresa, 4 figure di legge), Esito, almeno 1 Presenza, Meteo + Temperatura, Gestione Interferenze.
- Probabilmente sì: almeno 1 Attività in corso selezionata.
- Discutibile: Foto (può essere zero?), Prescrizioni (può essere "nulla da osservare"?).
- **Raccomandazione**: rendere obbligatorie solo le sezioni "anagrafiche" + esito + meteo + presenze + interferenze + attività. Foto e prescrizioni opzionali. **Conferma o specifica diversamente.**

---

## 10. Implicazioni Blazor Server + uso in cantiere

### 10.1 Configurazione circuit per connettività instabile

Default ASP.NET Core (per riferimento):
```
DisconnectedCircuitMaxRetained:    100
DisconnectedCircuitRetentionPeriod: 3 minuti
JSInteropDefaultCallTimeout:       1 minuto
MaxBufferedUnacknowledgedRenderBatches: 10
```

Proposta tarata su uso in cantiere:
```csharp
services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitMaxRetained = 200;             // raddoppio: più utenti potenzialmente disconnessi
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);  // da 3 a 15 min
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);          // da 1 a 2 min
        options.MaxBufferedUnacknowledgedRenderBatches = 20;      // più tolleranza a latenza
    });
```

Razionale: 15 minuti di retention coprono uno spostamento dentro al cantiere (rientro al mezzo, perdita 4G temporanea, attraversamento di zona schermata). 200 circuit sospesi è abbondante: 50 CSE che ognuno ha 2-4 dispositivi = 200. JSInterop a 2 min copre l'upload foto su rete lenta.

### 10.2 Strategia salvataggio bozza

**Combinazione di entrambi**:
- **Salva su transizione di step**: ogni "Avanti →" persiste lo stato corrente. Garantito.
- **Auto-save con debounce**: ogni 8 secondi di inattività (o 30s di attività continua), invio del delta dello step corrente. Visibile come "Salvato 12s fa" nel footer.
- **Salva su blur** dei campi-testo lunghi (textarea sez. 8): garantisce che dopo 3 minuti di scrittura il contenuto sia su DB anche senza cambio step.

**Pro/contro**:
- Solo per step (proposta originale): perdita potenziale di 5+ minuti di lavoro se l'utente entra in una textarea, scrive, e perde la connessione.
- Solo debounce: traffico SignalR continuo.
- Combinato (raccomandato): copertura massima con costi accettabili.

**Granularità invio**: invio solo del delta dello step (non l'intero verbale ogni volta). Il `VerbaleManager` ha metodi specifici tipo `AggiornaSezione3Async(verbaleId, attivitaSelezionate)`. Riduce il payload e abilita cache lato server.

### 10.3 Reconnect UX

Quando l'utente perde connessione e poi rientra:
- Il banner di reconnect Blazor di default (modale "tentando di riconnettersi...") va personalizzato in italiano e con il design di MudBlazor (sostituendo `ReconnectModal.razor` già presente nel progetto).
- Se la riconnessione riesce entro la `DisconnectedCircuitRetentionPeriod` (15 min): tutto torna trasparente, l'utente è nello stesso step con i dati salvati.
- Se la riconnessione fallisce (circuit drop): l'utente è rediretto a `/`. La homepage **deve avere una sezione "Verbali in bozza"** in evidenza, con link diretto a riprendere ciascuno: `/verbali/{id}?step=N`.

**Combinazione raccomandata**: sia route con id (RF-04 implicito) sia sezione "verbali in bozza" in homepage. Il primo serve a riprendere uno specifico verbale dopo reconnect; la seconda è la rete di sicurezza.

### 10.4 Conferma compatibilità MudBlazor + InteractiveServer

Verificato in §8.1 contro la documentazione ufficiale MudBlazor: **InteractiveServer è il render mode predefinito documentato e supportato**. Setup descritto, nessun problema noto in 9.x. Mitigazioni per dialog/popover (prerender:false) note e applicabili.

### 10.5 InteractiveAuto / WebAssembly come opzione futura

**Nota esplicita di non-scope**: l'app gira esclusivamente in InteractiveServer in v1. InteractiveAuto (server prima, poi WASM lato client) o WebAssembly puro sono opzioni che valuteremo solo se emergono requisiti offline non gestibili altrimenti. **Non** li perseguiamo ora: introducono complessità significativa (assembly trimming, double codepath, HttpClient invece di iniezione diretta dei Manager) senza beneficio per l'use case attuale.

---

## Note per futuro aggiornamento di CLAUDE.md

> Pattern emersi durante la stesura di questo doc che, una volta consolidati in codice, varrà la pena codificare come convenzioni in `CLAUDE.md`. **Da fare in Fase B o successive, non in questa sessione.**

1. **Pattern catalogo + IsAttivo**: tutte le entità di catalogo (TipoAttivita, TipoDocumento, ecc.) seguono `Id/Codice/Etichetta/Ordine/IsAttivo`. Nessun hard delete, solo `IsAttivo=false`. Codice immutabile (dominio: chiave funzionale stabile).
2. **Pattern aggregato `Verbale`**: tutte le entità figlie (`Presenza`, `Foto`, `VerbaleXxx`, ecc.) hanno una sola FK verso `Verbale` con cascade. Manager singolo (`VerbaleManager`) orchestra le N modifiche dentro a una transazione `IDbTransaction`.
3. **Anagrafiche con `IsAttivo`** (Cantiere/Committente/Impresa/Persona/Utente): mai hard delete, sempre `IsAttivo=false`. Soft delete vero (`IsDeleted` + `DeletedAt`) solo su `Verbale` per il vincolo legale.
4. **Repository → Manager 1:1**: ribadire che `VerbaleRepository` ↔ `VerbaleManager`, `CantiereRepository` ↔ `CantiereManager`, ecc. Nessun "shared service" trasversale.
5. **Dapper + transazioni**: ogni operazione che tocca > 1 tabella va in `IDbTransaction` aperta nel Manager, non nel Repository. I Repository ricevono opzionalmente la transazione.
6. **Storage astratto via `IFotoStorageService`**: niente accesso diretto al filesystem dai Manager. La factory implementativa è registrata in DI in base ad appsettings.
7. **CSS**: solo `wwwroot/css/app.css` per override globali + `.razor.css` co-locati per stili scope-locale del componente. Niente `<style>` inline.
8. **Tema MudBlazor in C#**, mai SCSS.
9. **Numero verbale assegnato a transizione di stato**, non in creazione (per non bruciare numeri su bozze cancellate).
10. **Validazione su due livelli**: "soft" per salva bozza (parsing/tipi), "hard" per transizione di stato (requisiti completi).

---

## Addendum 2026-05-05 — Raffinature emerse in B.3 (entità POCO)

Durante la scrittura delle entità sono emerse tre raffinature di naming che integrano (non sostituiscono) le sezioni §2.1 e §3.5. Nessun impatto architetturale, solo nomi.

1. **Nullabilità dei campi compilabili sul `Verbale`**. `Esito`, `Meteo`, `Interferenze`, `TemperaturaCelsius`, `Numero`, `Anno` sono nullable in C# (e saranno `NULL` permessi in DB nella migration di B.4). Una `Bozza` può avere campi non ancora compilati: la validazione "hard" alla transizione `Bozza → FirmatoCse` (§9.22) garantisce che alla firma siano tutti valorizzati. `Stato` resta NOT NULL con default `Bozza`.

2. **Naming proprietà enum-tipate**. Per evitare collisione tra nome proprietà e nome del tipo enum in C# (es. `Verbale.EsitoVerifica` con tipo `EsitoVerifica` rende ambigui i riferimenti dentro la classe), le proprietà sul `Verbale` sono: `Esito` (tipo `EsitoVerifica?`), `Meteo` (tipo `CondizioneMeteo?`), `Interferenze` (tipo `GestioneInterferenze?`), `Stato` (tipo `StatoVerbale`).

3. **Naming FK alle figure di legge**. Uniformati agli acronimi del PDF: `RlPersonaId`, `CspPersonaId`, `CsePersonaId`, `DlPersonaId` (al posto del `RuoloLavoriPersonaId` / `CsPersonaId` di §2.1, dove "CsPersonaId" era anche un refuso per "CspPersonaId").

4. **PascalCase di acronimi a 3+ lettere**: `PrescrizioneCse` (non `PrescrizioneCSE`) seguendo la convenzione C# (`Cse` come `Url`/`Xml`/`Json`).

5. **Conteggio tabelle**: §3.7 stimava "~14 tabelle". Il numero effettivo è **19** (5 anagrafiche + 4 cataloghi + 1 Verbale + 9 figlie inclusa `VerbaleAudit` aggiunta per §9.12). La discrepanza è solo di sintesi: tutte le 19 entità sono coerenti con §2.1 e sono state create dalla migration `001_InitialSchema.sql`.

6. **Dapper + `DateOnly`** (B.5). Dapper 2.1.72 NON ha mapping nativo per `System.DateOnly`/`DateOnly?` come parametro: alza `NotSupportedException` al primo `INSERT`. Risolto con `Data/DapperConfiguration.cs` (TypeHandler custom + variante nullable, idempotente, invocato in `Program.cs` e in `tests/.../TestSqlConnectionFactory` static ctor). Pattern da estendere se in futuro aggiungiamo `TimeOnly` o tipi custom.

---

## Stato del documento

- **Sezioni 1-8, 10**: approvate implicitamente (nessuna contestazione).
- **Sezione 9**: ✅ **22/22 voci approvate il 2026-05-05.**
- **Sotto-fasi B**: B.1 / B.2 / B.3 completate. **B.4 completata 2026-05-05**: `ICMVerbaliDb` creato su `.\SQLEXPRESS`, 19 tabelle + 31 voci di seed applicate via `Invoke-Sqlcmd`. **B.5 completata 2026-05-05**: 10 Repository Dapper + 10 Manager + DI Scoped + 10 smoke test xUnit (10/10 pass). Fix Dapper-`DateOnly` documentato in Addendum. **B.6 completata 2026-05-05**: cookie auth, login/logout Minimal API, `IPasswordHasherService` (PBKDF2 via Identity), `DatabaseSeeder` `IHostedService` idempotente, policy `RequireAdmin`. **Zero NuGet aggiuntivi** (`PasswordHasher<TUser>` arriva da framework reference). Login flow verificato end-to-end via curl.

**Documento congelato come baseline di design.** Eventuali deviazioni emerse in implementazione devono aggiornare questo file in modo additivo (vedi CLAUDE.md "Documento vivo"). Si procede con la Fase B secondo il piano concordato in chat.
