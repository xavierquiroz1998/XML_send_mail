-- ElectronicDocuments
CREATE TABLE IF NOT EXISTS ElectronicDocuments (
    Id                      TEXT      PRIMARY KEY,
    Type                    INTEGER   NOT NULL,
    AccessKey               TEXT      NOT NULL,
    DocumentNumber          TEXT      NOT NULL,
    IssueDate               TEXT      NOT NULL,
    Environment             TEXT      NOT NULL,

    Issuer_Ruc              TEXT      NOT NULL,
    Issuer_BusinessName     TEXT      NOT NULL,
    Issuer_CommercialName   TEXT      NULL,
    Issuer_Address          TEXT      NULL,

    Receiver_IdType         TEXT      NOT NULL,
    Receiver_Id             TEXT      NOT NULL,
    Receiver_Name           TEXT      NOT NULL,
    Receiver_Email          TEXT      NULL,
    Receiver_Phone          TEXT      NULL,
    Receiver_Address        TEXT      NULL,

    Subtotal                NUMERIC   NOT NULL,
    Taxes                   NUMERIC   NOT NULL,
    Total                   NUMERIC   NOT NULL,
    OriginalXml             TEXT      NOT NULL,

    CreatedAt               TEXT      NOT NULL,
    UpdatedAt               TEXT      NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_ElectronicDocuments_AccessKey ON ElectronicDocuments(AccessKey);
CREATE INDEX IF NOT EXISTS IX_ElectronicDocuments_IssueDate ON ElectronicDocuments(IssueDate);

-- DocumentLines
CREATE TABLE IF NOT EXISTS DocumentLines (
    Id                      TEXT      PRIMARY KEY,
    ElectronicDocumentId    TEXT      NOT NULL REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
    Code                    TEXT      NULL,
    Description             TEXT      NOT NULL,
    Quantity                NUMERIC   NOT NULL,
    UnitPrice               NUMERIC   NOT NULL,
    Discount                NUMERIC   NOT NULL,
    Subtotal                NUMERIC   NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_DocumentLines_DocumentId ON DocumentLines(ElectronicDocumentId);

-- EmailLogs
CREATE TABLE IF NOT EXISTS EmailLogs (
    Id                      TEXT      PRIMARY KEY,
    ElectronicDocumentId    TEXT      NOT NULL REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
    RecipientEmail          TEXT      NOT NULL,
    Subject                 TEXT      NOT NULL,
    Status                  INTEGER   NOT NULL,
    ErrorMessage            TEXT      NULL,
    SentAt                  TEXT      NULL,
    CreatedAt               TEXT      NOT NULL,
    UpdatedAt               TEXT      NULL
);
CREATE INDEX IF NOT EXISTS IX_EmailLogs_DocumentId ON EmailLogs(ElectronicDocumentId);
CREATE INDEX IF NOT EXISTS IX_EmailLogs_Status ON EmailLogs(Status);
CREATE INDEX IF NOT EXISTS IX_EmailLogs_SentAt ON EmailLogs(SentAt);

-- SmtpConfigurations
CREATE TABLE IF NOT EXISTS SmtpConfigurations (
    Id                      TEXT      PRIMARY KEY,
    Name                    TEXT      NOT NULL,
    Host                    TEXT      NOT NULL,
    Port                    INTEGER   NOT NULL,
    UseSsl                  INTEGER   NOT NULL,
    Username                TEXT      NOT NULL,
    EncryptedPassword       TEXT      NOT NULL,
    FromEmail               TEXT      NOT NULL,
    FromName                TEXT      NOT NULL,
    IsActive                INTEGER   NOT NULL,
    CreatedAt               TEXT      NOT NULL,
    UpdatedAt               TEXT      NULL
);
