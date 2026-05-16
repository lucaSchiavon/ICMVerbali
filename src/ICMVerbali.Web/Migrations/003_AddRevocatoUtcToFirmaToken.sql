-- =====================================================================
-- 003_AddRevocatoUtcToFirmaToken.sql
-- Aggiunge la colonna RevocatoUtc alla tabella FirmaToken (B.12).
-- Permette al CSE di rigenerare il magic-link dell'Impresa invalidando i
-- token attivi precedenti senza eliminare la riga (audit trail).
-- Vedi docs/01-design.md Addendum B.12 (rigenerazione token impresa).
--
-- Compatibile con SQL Server 2019+ / Azure SQL Database.
-- Da eseguire una volta sola su database con migration 002 gia' applicata.
--
-- Esempio applicazione (PowerShell):
--   sqlcmd -S .\SQLEXPRESS -d ICMVerbaliDb -I -i src/ICMVerbali.Web/Migrations/003_AddRevocatoUtcToFirmaToken.sql
--
-- Nota: opzione -I obbligatoria (QUOTED_IDENTIFIER ON), vedi memoria
-- sqlcmd_quoted_identifier.md.
-- =====================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

ALTER TABLE dbo.FirmaToken
    ADD RevocatoUtc datetime2(0) NULL;
GO

-- =====================================================================
-- Fine 003_AddRevocatoUtcToFirmaToken.sql
-- =====================================================================

PRINT 'Migrazione 003_AddRevocatoUtcToFirmaToken applicata con successo.';
GO
