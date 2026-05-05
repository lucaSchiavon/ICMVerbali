-- =====================================================================
-- 001_InitialSchema.sql
-- Schema iniziale ICMVerbali. Vedi docs/01-design.md §3.
--
-- Compatibile con SQL Server 2019+ / Azure SQL Database.
-- Da eseguire UNA SOLA VOLTA su database vuoto. Il database stesso
-- (ICMVerbaliDb di default) deve esistere prima dell'esecuzione.
--
-- Esempio applicazione (PowerShell):
--   sqlcmd -S .\SQLEXPRESS -Q "IF DB_ID('ICMVerbaliDb') IS NULL CREATE DATABASE ICMVerbaliDb;"
--   sqlcmd -S .\SQLEXPRESS -d ICMVerbaliDb -i src/ICMVerbali.Web/Migrations/001_InitialSchema.sql
--
-- Rollback rapido (in dev): DROP DATABASE ICMVerbaliDb e ricrea.
-- =====================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- =====================================================================
-- 1. ANAGRAFICHE (vita propria, IsAttivo per disattivazione logica)
-- =====================================================================

CREATE TABLE dbo.Utente
(
    Id              uniqueidentifier NOT NULL CONSTRAINT PK_Utente PRIMARY KEY,
    Username        nvarchar(80)     NOT NULL,
    Email           nvarchar(200)    NULL,
    PasswordHash    nvarchar(200)    NOT NULL,
    Ruolo           tinyint          NOT NULL,
    IsAttivo        bit              NOT NULL CONSTRAINT DF_Utente_IsAttivo DEFAULT 1,
    CreatedAt       datetime2(3)     NOT NULL CONSTRAINT DF_Utente_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       datetime2(3)     NOT NULL CONSTRAINT DF_Utente_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Utente_Username UNIQUE (Username)
);
GO

-- Email opzionale ma se presente deve essere unica.
CREATE UNIQUE INDEX UQ_Utente_Email
    ON dbo.Utente (Email)
    WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.Cantiere
(
    Id              uniqueidentifier NOT NULL CONSTRAINT PK_Cantiere PRIMARY KEY,
    Ubicazione      nvarchar(300)    NOT NULL,
    Tipologia       nvarchar(500)    NOT NULL,
    ImportoAppalto  decimal(18, 2)   NULL,
    IsAttivo        bit              NOT NULL CONSTRAINT DF_Cantiere_IsAttivo DEFAULT 1
);
GO

CREATE INDEX IX_Cantiere_IsAttivo ON dbo.Cantiere (IsAttivo);
GO

CREATE TABLE dbo.Committente
(
    Id                              uniqueidentifier NOT NULL CONSTRAINT PK_Committente PRIMARY KEY,
    RagioneSociale                  nvarchar(250)    NOT NULL,
    Indirizzo                       nvarchar(400)    NULL,
    CodiceFiscale                   nvarchar(20)     NULL,
    PartitaIva                      nvarchar(20)     NULL,
    NumeroIscrizioneRegistroImprese nvarchar(80)     NULL,
    IsAttivo                        bit              NOT NULL CONSTRAINT DF_Committente_IsAttivo DEFAULT 1
);
GO

CREATE INDEX IX_Committente_RagioneSociale ON dbo.Committente (RagioneSociale);
GO

CREATE TABLE dbo.ImpresaAppaltatrice
(
    Id                              uniqueidentifier NOT NULL CONSTRAINT PK_ImpresaAppaltatrice PRIMARY KEY,
    RagioneSociale                  nvarchar(250)    NOT NULL,
    Indirizzo                       nvarchar(400)    NULL,
    CodiceFiscale                   nvarchar(20)     NULL,
    PartitaIva                      nvarchar(20)     NULL,
    NumeroIscrizioneRegistroImprese nvarchar(80)     NULL,
    IsAttivo                        bit              NOT NULL CONSTRAINT DF_ImpresaAppaltatrice_IsAttivo DEFAULT 1
);
GO

CREATE INDEX IX_ImpresaAppaltatrice_RagioneSociale ON dbo.ImpresaAppaltatrice (RagioneSociale);
GO

CREATE TABLE dbo.Persona
(
    Id          uniqueidentifier NOT NULL CONSTRAINT PK_Persona PRIMARY KEY,
    Nominativo  nvarchar(200)    NOT NULL,
    Azienda     nvarchar(200)    NOT NULL,
    IsAttivo    bit              NOT NULL CONSTRAINT DF_Persona_IsAttivo DEFAULT 1
);
GO

CREATE INDEX IX_Persona_Nominativo ON dbo.Persona (Nominativo);
GO

-- =====================================================================
-- 2. CATALOGHI (immutabili, Codice = chiave funzionale stabile)
-- =====================================================================

CREATE TABLE dbo.CatalogoTipoAttivita
(
    Id          uniqueidentifier NOT NULL CONSTRAINT PK_CatalogoTipoAttivita PRIMARY KEY,
    Codice      varchar(80)      NOT NULL,
    Etichetta   nvarchar(200)    NOT NULL,
    Ordine      int              NOT NULL,
    IsAttivo    bit              NOT NULL CONSTRAINT DF_CatalogoTipoAttivita_IsAttivo DEFAULT 1,
    CONSTRAINT UQ_CatalogoTipoAttivita_Codice UNIQUE (Codice)
);
GO

CREATE TABLE dbo.CatalogoTipoDocumento
(
    Id          uniqueidentifier NOT NULL CONSTRAINT PK_CatalogoTipoDocumento PRIMARY KEY,
    Codice      varchar(80)      NOT NULL,
    Etichetta   nvarchar(200)    NOT NULL,
    Ordine      int              NOT NULL,
    IsAttivo    bit              NOT NULL CONSTRAINT DF_CatalogoTipoDocumento_IsAttivo DEFAULT 1,
    CONSTRAINT UQ_CatalogoTipoDocumento_Codice UNIQUE (Codice)
);
GO

CREATE TABLE dbo.CatalogoTipoApprestamento
(
    Id              uniqueidentifier NOT NULL CONSTRAINT PK_CatalogoTipoApprestamento PRIMARY KEY,
    Codice          varchar(80)      NOT NULL,
    Etichetta       nvarchar(200)    NOT NULL,
    Ordine          int              NOT NULL,
    IsAttivo        bit              NOT NULL CONSTRAINT DF_CatalogoTipoApprestamento_IsAttivo DEFAULT 1,
    Sottosezione    tinyint          NOT NULL,  -- 1=5.1, 2=5.2, 3=5.3, 4=5.4
    CONSTRAINT UQ_CatalogoTipoApprestamento_Codice UNIQUE (Codice)
);
GO

CREATE TABLE dbo.CatalogoTipoCondizioneAmbientale
(
    Id          uniqueidentifier NOT NULL CONSTRAINT PK_CatalogoTipoCondizioneAmbientale PRIMARY KEY,
    Codice      varchar(80)      NOT NULL,
    Etichetta   nvarchar(200)    NOT NULL,
    Ordine      int              NOT NULL,
    IsAttivo    bit              NOT NULL CONSTRAINT DF_CatalogoTipoCondizioneAmbientale_IsAttivo DEFAULT 1,
    CONSTRAINT UQ_CatalogoTipoCondizioneAmbientale_Codice UNIQUE (Codice)
);
GO

-- =====================================================================
-- 3. AGGREGATE ROOT: VERBALE
-- =====================================================================

CREATE TABLE dbo.Verbale
(
    Id                          uniqueidentifier NOT NULL CONSTRAINT PK_Verbale PRIMARY KEY,

    -- Numero e Anno assegnati alla transizione Bozza -> FirmatoCse (vedi §9.10).
    Numero                      int              NULL,
    Anno                        int              NULL,

    Data                        date             NOT NULL,

    CantiereId                  uniqueidentifier NOT NULL,
    CommittenteId               uniqueidentifier NOT NULL,
    ImpresaAppaltatriceId       uniqueidentifier NOT NULL,

    -- 4 figure di legge: stesso target Persona, FK distinte.
    RlPersonaId                 uniqueidentifier NOT NULL,
    CspPersonaId                uniqueidentifier NOT NULL,
    CsePersonaId                uniqueidentifier NOT NULL,
    DlPersonaId                 uniqueidentifier NOT NULL,

    -- Campi compilabili durante la stesura: nullable per Bozze incomplete.
    Esito                       tinyint          NULL,
    Meteo                       tinyint          NULL,
    TemperaturaCelsius          int              NULL,
    Interferenze                tinyint          NULL,
    InterferenzeNote            nvarchar(1000)   NULL,

    Stato                       tinyint          NOT NULL CONSTRAINT DF_Verbale_Stato DEFAULT 0,

    CompilatoDaUtenteId         uniqueidentifier NOT NULL,

    -- Soft delete logico (verbali firmati = documenti legali, vedi §9.6).
    IsDeleted                   bit              NOT NULL CONSTRAINT DF_Verbale_IsDeleted DEFAULT 0,
    DeletedAt                   datetime2(3)     NULL,

    CreatedAt                   datetime2(3)     NOT NULL CONSTRAINT DF_Verbale_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt                   datetime2(3)     NOT NULL CONSTRAINT DF_Verbale_UpdatedAt DEFAULT SYSUTCDATETIME(),

    -- FK verso anagrafiche: NO ACTION (le anagrafiche si disattivano, non si cancellano).
    CONSTRAINT FK_Verbale_Cantiere
        FOREIGN KEY (CantiereId) REFERENCES dbo.Cantiere (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_Committente
        FOREIGN KEY (CommittenteId) REFERENCES dbo.Committente (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_ImpresaAppaltatrice
        FOREIGN KEY (ImpresaAppaltatriceId) REFERENCES dbo.ImpresaAppaltatrice (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_Persona_Rl
        FOREIGN KEY (RlPersonaId) REFERENCES dbo.Persona (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_Persona_Csp
        FOREIGN KEY (CspPersonaId) REFERENCES dbo.Persona (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_Persona_Cse
        FOREIGN KEY (CsePersonaId) REFERENCES dbo.Persona (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_Persona_Dl
        FOREIGN KEY (DlPersonaId) REFERENCES dbo.Persona (Id)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Verbale_Utente_CompilatoDa
        FOREIGN KEY (CompilatoDaUtenteId) REFERENCES dbo.Utente (Id)
        ON DELETE NO ACTION
);
GO

-- UNIQUE filtrato: bozze (Anno/Numero NULL) coesistono senza occupare numerazione.
CREATE UNIQUE INDEX UQ_Verbale_Anno_Numero
    ON dbo.Verbale (Anno, Numero)
    WHERE Anno IS NOT NULL AND Numero IS NOT NULL;
GO

CREATE INDEX IX_Verbale_Data ON dbo.Verbale (Data);
GO

-- Bozze in evidenza: indice filtrato sulle bozze non eliminate.
CREATE INDEX IX_Verbale_Bozze
    ON dbo.Verbale (UpdatedAt DESC)
    WHERE Stato = 0 AND IsDeleted = 0;
GO

CREATE INDEX IX_Verbale_CantiereId ON dbo.Verbale (CantiereId);
GO

-- =====================================================================
-- 4. ENTITA' FIGLIE DEL VERBALE (cascade delete su VerbaleId)
-- =====================================================================

CREATE TABLE dbo.Presenza
(
    Id                  uniqueidentifier NOT NULL CONSTRAINT PK_Presenza PRIMARY KEY,
    VerbaleId           uniqueidentifier NOT NULL,
    PersonaId           uniqueidentifier NULL,
    NominativoLibero    nvarchar(200)    NULL,
    ImpresaLibera       nvarchar(200)    NULL,
    Ordine              int              NOT NULL,

    CONSTRAINT FK_Presenza_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Presenza_Persona
        FOREIGN KEY (PersonaId) REFERENCES dbo.Persona (Id)
        ON DELETE NO ACTION,

    CONSTRAINT CK_Presenza_PersonaOrLibero CHECK (
        PersonaId IS NOT NULL OR NominativoLibero IS NOT NULL
    )
);
GO

CREATE INDEX IX_Presenza_VerbaleId ON dbo.Presenza (VerbaleId);
GO

CREATE TABLE dbo.VerbaleAttivita
(
    VerbaleId               uniqueidentifier NOT NULL,
    CatalogoTipoAttivitaId  uniqueidentifier NOT NULL,
    Selezionato             bit              NOT NULL CONSTRAINT DF_VerbaleAttivita_Selezionato DEFAULT 0,
    AltroDescrizione        nvarchar(300)    NULL,

    CONSTRAINT PK_VerbaleAttivita PRIMARY KEY (VerbaleId, CatalogoTipoAttivitaId),
    CONSTRAINT FK_VerbaleAttivita_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_VerbaleAttivita_CatalogoTipoAttivita
        FOREIGN KEY (CatalogoTipoAttivitaId) REFERENCES dbo.CatalogoTipoAttivita (Id)
        ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.VerbaleDocumento
(
    VerbaleId                   uniqueidentifier NOT NULL,
    CatalogoTipoDocumentoId     uniqueidentifier NOT NULL,
    Applicabile                 bit              NOT NULL CONSTRAINT DF_VerbaleDocumento_Applicabile DEFAULT 0,
    Conforme                    bit              NOT NULL CONSTRAINT DF_VerbaleDocumento_Conforme DEFAULT 0,
    Note                        nvarchar(500)    NULL,
    AltroDescrizione            nvarchar(300)    NULL,

    CONSTRAINT PK_VerbaleDocumento PRIMARY KEY (VerbaleId, CatalogoTipoDocumentoId),
    CONSTRAINT FK_VerbaleDocumento_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_VerbaleDocumento_CatalogoTipoDocumento
        FOREIGN KEY (CatalogoTipoDocumentoId) REFERENCES dbo.CatalogoTipoDocumento (Id)
        ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.VerbaleApprestamento
(
    VerbaleId                       uniqueidentifier NOT NULL,
    CatalogoTipoApprestamentoId     uniqueidentifier NOT NULL,
    Applicabile                     bit              NOT NULL CONSTRAINT DF_VerbaleApprestamento_Applicabile DEFAULT 0,
    Conforme                        bit              NOT NULL CONSTRAINT DF_VerbaleApprestamento_Conforme DEFAULT 0,
    Note                            nvarchar(500)    NULL,

    CONSTRAINT PK_VerbaleApprestamento PRIMARY KEY (VerbaleId, CatalogoTipoApprestamentoId),
    CONSTRAINT FK_VerbaleApprestamento_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_VerbaleApprestamento_CatalogoTipoApprestamento
        FOREIGN KEY (CatalogoTipoApprestamentoId) REFERENCES dbo.CatalogoTipoApprestamento (Id)
        ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.VerbaleCondizioneAmbientale
(
    VerbaleId                               uniqueidentifier NOT NULL,
    CatalogoTipoCondizioneAmbientaleId      uniqueidentifier NOT NULL,
    Conforme                                bit              NOT NULL CONSTRAINT DF_VerbaleCondAmb_Conforme DEFAULT 0,
    NonConforme                             bit              NOT NULL CONSTRAINT DF_VerbaleCondAmb_NonConforme DEFAULT 0,
    Note                                    nvarchar(500)    NULL,

    CONSTRAINT PK_VerbaleCondizioneAmbientale PRIMARY KEY (VerbaleId, CatalogoTipoCondizioneAmbientaleId),
    CONSTRAINT FK_VerbaleCondAmb_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_VerbaleCondAmb_CatalogoTipoCondizioneAmbientale
        FOREIGN KEY (CatalogoTipoCondizioneAmbientaleId) REFERENCES dbo.CatalogoTipoCondizioneAmbientale (Id)
        ON DELETE NO ACTION,

    -- Conforme XOR NonConforme: vincolo logico che riflette il PDF (sez. 6).
    CONSTRAINT CK_VerbaleCondAmb_ConformeXorNonConforme CHECK (
        NOT (Conforme = 1 AND NonConforme = 1)
    )
);
GO

CREATE TABLE dbo.PrescrizioneCse
(
    Id          uniqueidentifier NOT NULL CONSTRAINT PK_PrescrizioneCse PRIMARY KEY,
    VerbaleId   uniqueidentifier NOT NULL,
    Testo       nvarchar(2000)   NOT NULL,
    Ordine      int              NOT NULL,

    CONSTRAINT FK_PrescrizioneCse_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE
);
GO

CREATE INDEX IX_PrescrizioneCse_VerbaleId ON dbo.PrescrizioneCse (VerbaleId);
GO

CREATE TABLE dbo.Foto
(
    Id                  uniqueidentifier NOT NULL CONSTRAINT PK_Foto PRIMARY KEY,
    VerbaleId           uniqueidentifier NOT NULL,
    FilePathRelativo    nvarchar(500)    NOT NULL,
    Didascalia          nvarchar(500)    NULL,
    Ordine              int              NOT NULL,
    CreatedAt           datetime2(3)     NOT NULL CONSTRAINT DF_Foto_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_Foto_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE
);
GO

CREATE INDEX IX_Foto_VerbaleId ON dbo.Foto (VerbaleId);
GO

CREATE TABLE dbo.Firma
(
    Id                  uniqueidentifier NOT NULL CONSTRAINT PK_Firma PRIMARY KEY,
    VerbaleId           uniqueidentifier NOT NULL,
    Tipo                tinyint          NOT NULL,  -- 0=Cse, 1=ImpresaAppaltatrice
    NomeFirmatario      nvarchar(200)    NOT NULL,
    DataFirma           date             NOT NULL,
    ImmagineFirmaPath   nvarchar(500)    NULL,

    CONSTRAINT FK_Firma_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT UQ_Firma_VerbaleId_Tipo UNIQUE (VerbaleId, Tipo)
);
GO

CREATE TABLE dbo.VerbaleAudit
(
    Id              uniqueidentifier NOT NULL CONSTRAINT PK_VerbaleAudit PRIMARY KEY,
    VerbaleId       uniqueidentifier NOT NULL,
    UtenteId        uniqueidentifier NOT NULL,
    DataEvento      datetime2(3)     NOT NULL CONSTRAINT DF_VerbaleAudit_DataEvento DEFAULT SYSUTCDATETIME(),
    EventoTipo      tinyint          NOT NULL,
    Note            nvarchar(1000)   NULL,

    CONSTRAINT FK_VerbaleAudit_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_VerbaleAudit_Utente
        FOREIGN KEY (UtenteId) REFERENCES dbo.Utente (Id)
        ON DELETE NO ACTION
);
GO

CREATE INDEX IX_VerbaleAudit_VerbaleId ON dbo.VerbaleAudit (VerbaleId);
GO

-- =====================================================================
-- 5. SEED DEI CATALOGHI (idempotente: IF NOT EXISTS su Codice)
-- I valori riflettono il PDF Verbale_sicurezza.pdf alla data 2026-05-05.
-- =====================================================================

-- Sez. 3 PDF: Attivita' in corso (16 voci).
DECLARE @SeedAttivita TABLE (Codice varchar(80) PRIMARY KEY, Etichetta nvarchar(200), Ordine int);
INSERT INTO @SeedAttivita (Codice, Etichetta, Ordine) VALUES
    ('ATTIVITA_ALLESTIMENTO_SMOBILIZZO',         N'Allestimento/Smobilizzo',          1),
    ('ATTIVITA_DEMOLIZIONI_RIMOZIONI',           N'Demolizioni/Rimozioni',            2),
    ('ATTIVITA_SCAVI_MOVIMENTI_TERRA',           N'Scavi/Movimenti terra',            3),
    ('ATTIVITA_FONDAZIONI_OPERE_CA',             N'Fondazioni/Opere C.A.',            4),
    ('ATTIVITA_STRUTTURE_PREFABBRICATE',         N'Strutture Prefabbricate',          5),
    ('ATTIVITA_CARPENTERIA_METALLICA',           N'Carpenteria Metallica',            6),
    ('ATTIVITA_TAMPONATURE_MURATURE',            N'Tamponature/Murature',             7),
    ('ATTIVITA_COPERTURE_IMPERMEABILIZZAZIONI',  N'Coperture/Impermeabilizzazioni',   8),
    ('ATTIVITA_SERRAMENTI_INFISSI',              N'Serramenti e Infissi',             9),
    ('ATTIVITA_IMPIANTI_ELETTRICI',              N'Impianti Elettrici',              10),
    ('ATTIVITA_IMPIANTI_MECCANICI_IDRAULICI',    N'Impianti Meccanici/Idraulici',    11),
    ('ATTIVITA_PAVIMENTAZIONI',                  N'Pavimentazioni',                  12),
    ('ATTIVITA_TINTEGGIATURE',                   N'Tinteggiature',                   13),
    ('ATTIVITA_FINITURE_CARTONGESSI',            N'Finiture/Cartongessi',            14),
    ('ATTIVITA_OPERE_ESTERNE_VERDE',             N'Opere Esterne/Verde',             15),
    ('ATTIVITA_ALTRO',                           N'Altro',                           16);

INSERT INTO dbo.CatalogoTipoAttivita (Id, Codice, Etichetta, Ordine, IsAttivo)
SELECT NEWID(), s.Codice, s.Etichetta, s.Ordine, 1
FROM @SeedAttivita s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CatalogoTipoAttivita c WHERE c.Codice = s.Codice
);
GO

-- Sez. 4 PDF: Verifica documentazione (4 voci).
DECLARE @SeedDocumento TABLE (Codice varchar(80) PRIMARY KEY, Etichetta nvarchar(200), Ordine int);
INSERT INTO @SeedDocumento (Codice, Etichetta, Ordine) VALUES
    ('DOC_NOTIFICA_PRELIMINARE',         N'Notifica Preliminare',           1),
    ('DOC_LIBRETTI_PONTEGGI_PIMUS',      N'Libretti Ponteggi / PIMUS',      2),
    ('DOC_FASCICOLI_MACCHINE_ATTREZZ',   N'Fascicoli Macchine/Attrezzature',3),
    ('DOC_ALTRO',                        N'Altro',                          4);

INSERT INTO dbo.CatalogoTipoDocumento (Id, Codice, Etichetta, Ordine, IsAttivo)
SELECT NEWID(), s.Codice, s.Etichetta, s.Ordine, 1
FROM @SeedDocumento s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CatalogoTipoDocumento c WHERE c.Codice = s.Codice
);
GO

-- Sez. 5 PDF: Apprestamenti e sicurezza (7 voci, raggruppate in 4 sottosezioni).
DECLARE @SeedApprestamento TABLE (
    Codice varchar(80) PRIMARY KEY,
    Etichetta nvarchar(200),
    Ordine int,
    Sottosezione tinyint
);
INSERT INTO @SeedApprestamento (Codice, Etichetta, Ordine, Sottosezione) VALUES
    -- 5.1 Organizzazione
    ('APPREST_RECINZIONE_CARTELLI_VIABILITA',        N'Recinzione, Cartelli, Viabilità',         1, 1),
    ('APPREST_STOCCAGGIO_RIFIUTI_SERVIZI',           N'Stoccaggio, Rifiuti, Servizi',            2, 1),
    -- 5.2 Cadute dall'alto
    ('APPREST_PONTEGGI',                             N'Ponteggi (montaggio/verifica)',           3, 2),
    ('APPREST_PARAPETTI_SCALE_LINEEVITA',            N'Parapetti, Scale, Linee Vita',            4, 2),
    -- 5.3 Emergenze & DPI
    ('APPREST_ESTINTORI_PRIMOSOCCORSO_VIEFUGA',      N'Estintori, Primo Soccorso, Vie fuga',     5, 3),
    ('APPREST_DPI',                                  N'Uso DPI (Caschi, Scarpe, ecc.)',          6, 3),
    -- 5.4 Impianti
    ('APPREST_IMPIANTO_ELETTRICO_CANTIERE',          N'Impianto Elettrico Cantiere',             7, 4);

INSERT INTO dbo.CatalogoTipoApprestamento (Id, Codice, Etichetta, Ordine, IsAttivo, Sottosezione)
SELECT NEWID(), s.Codice, s.Etichetta, s.Ordine, 1, s.Sottosezione
FROM @SeedApprestamento s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CatalogoTipoApprestamento c WHERE c.Codice = s.Codice
);
GO

-- Sez. 6 PDF: Condizioni ambientali (4 voci).
DECLARE @SeedCondAmb TABLE (Codice varchar(80) PRIMARY KEY, Etichetta nvarchar(200), Ordine int);
INSERT INTO @SeedCondAmb (Codice, Etichetta, Ordine) VALUES
    ('CONDAMB_ILLUMINAZIONE',  N'Illuminazione',    1),
    ('CONDAMB_POLVERI',        N'Polveri',          2),
    ('CONDAMB_RUMORE',         N'Rumore',           3),
    ('CONDAMB_PULIZIA_STRADE', N'Pulizia Strade',   4);

INSERT INTO dbo.CatalogoTipoCondizioneAmbientale (Id, Codice, Etichetta, Ordine, IsAttivo)
SELECT NEWID(), s.Codice, s.Etichetta, s.Ordine, 1
FROM @SeedCondAmb s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CatalogoTipoCondizioneAmbientale c WHERE c.Codice = s.Codice
);
GO

-- =====================================================================
-- Fine 001_InitialSchema.sql
-- =====================================================================

PRINT 'Migrazione 001_InitialSchema applicata con successo.';
GO
