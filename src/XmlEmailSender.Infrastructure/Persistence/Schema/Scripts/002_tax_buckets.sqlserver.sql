IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DocumentTaxBuckets')
BEGIN
    CREATE TABLE DocumentTaxBuckets (
        Id                   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ElectronicDocumentId UNIQUEIDENTIFIER NOT NULL
            FOREIGN KEY REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
        CodigoPorcentaje     NVARCHAR(10)     NOT NULL,
        BaseImponible        DECIMAL(18,4)    NOT NULL,
        Valor                DECIMAL(18,4)    NOT NULL
    );
    CREATE INDEX IX_DocumentTaxBuckets_DocumentId ON DocumentTaxBuckets(ElectronicDocumentId);
END
GO
