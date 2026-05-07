CREATE TABLE IF NOT EXISTS DocumentTaxBuckets (
    Id                   UUID            NOT NULL PRIMARY KEY,
    ElectronicDocumentId UUID            NOT NULL REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
    CodigoPorcentaje     VARCHAR(10)     NOT NULL,
    BaseImponible        NUMERIC(18,4)   NOT NULL,
    Valor                NUMERIC(18,4)   NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_documenttaxbuckets_documentid ON DocumentTaxBuckets(ElectronicDocumentId);
