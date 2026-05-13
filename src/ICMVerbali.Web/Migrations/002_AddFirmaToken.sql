-- =====================================================================
-- 002_AddFirmaToken.sql
-- Aggiunge la tabella FirmaToken per la firma Impresa via magic-link.
-- Vedi docs/01-design.md Addendum 2026-05-14 (B.11).
--
-- Compatibile con SQL Server 2019+ / Azure SQL Database.
-- Da eseguire una volta sola su database con migration 001 gia' applicata.
--
-- Esempio applicazione (PowerShell):
--   sqlcmd -S .\SQLEXPRESS -d ICMVerbaliDb -I -i src/ICMVerbali.Web/Migrations/002_AddFirmaToken.sql
--
-- Nota: opzione -I obbligatoria (QUOTED_IDENTIFIER ON) per coerenza col
-- comportamento di default delle connessioni applicative.
-- =====================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE TABLE dbo.FirmaToken
(
    Id              uniqueidentifier NOT NULL CONSTRAINT PK_FirmaToken PRIMARY KEY,
    VerbaleId       uniqueidentifier NOT NULL,
    -- Token GUID v7 distinto da Id: non espone la PK e permette eventuale
    -- rotazione/rigenerazione futura senza toccare la riga principale.
    Token           uniqueidentifier NOT NULL,
    ScadenzaUtc     datetime2(0)     NOT NULL,
    -- Uso singolo: popolato al momento del consumo (firma impresa).
    UsatoUtc        datetime2(0)     NULL,
    CreatedAt       datetime2(3)     NOT NULL CONSTRAINT DF_FirmaToken_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_FirmaToken_Verbale
        FOREIGN KEY (VerbaleId) REFERENCES dbo.Verbale (Id)
        ON DELETE CASCADE,

    CONSTRAINT UQ_FirmaToken_Token UNIQUE (Token)
);
GO

CREATE INDEX IX_FirmaToken_VerbaleId ON dbo.FirmaToken (VerbaleId);
GO

-- =====================================================================
-- Fine 002_AddFirmaToken.sql
-- =====================================================================

PRINT 'Migrazione 002_AddFirmaToken applicata con successo.';
GO
