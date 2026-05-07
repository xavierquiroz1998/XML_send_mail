CREATE TABLE IF NOT EXISTS DocumentTaxBuckets (
    Id                      TEXT      PRIMARY KEY,
    ElectronicDocumentId    TEXT      NOT NULL REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
    CodigoPorcentaje        TEXT      NOT NULL,
    BaseImponible           NUMERIC   NOT NULL,
    Valor                   NUMERIC   NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_DocumentTaxBuckets_DocumentId ON DocumentTaxBuckets(ElectronicDocumentId);
