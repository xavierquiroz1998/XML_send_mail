-- ElectronicDocuments
CREATE TABLE IF NOT EXISTS ElectronicDocuments (
    Id                    UUID            NOT NULL PRIMARY KEY,
    Type                  INTEGER         NOT NULL,
    AccessKey             VARCHAR(49)     NOT NULL,
    DocumentNumber        VARCHAR(30)     NOT NULL,
    IssueDate             TIMESTAMPTZ     NOT NULL,
    Environment           VARCHAR(20)     NOT NULL,

    Issuer_Ruc            VARCHAR(13)     NOT NULL,
    Issuer_BusinessName   VARCHAR(300)    NOT NULL,
    Issuer_CommercialName VARCHAR(300)    NULL,
    Issuer_Address        VARCHAR(300)    NULL,

    Receiver_IdType       VARCHAR(2)      NOT NULL,
    Receiver_Id           VARCHAR(20)     NOT NULL,
    Receiver_Name         VARCHAR(300)    NOT NULL,
    Receiver_Email        VARCHAR(255)    NULL,
    Receiver_Phone        VARCHAR(50)     NULL,
    Receiver_Address      VARCHAR(300)    NULL,

    Subtotal              NUMERIC(18,4)   NOT NULL,
    Taxes                 NUMERIC(18,4)   NOT NULL,
    Total                 NUMERIC(18,4)   NOT NULL,
    OriginalXml           TEXT            NOT NULL,

    CreatedAt             TIMESTAMPTZ     NOT NULL,
    UpdatedAt             TIMESTAMPTZ     NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_electronicdocuments_accesskey ON ElectronicDocuments(AccessKey);
CREATE INDEX IF NOT EXISTS ix_electronicdocuments_issuedate ON ElectronicDocuments(IssueDate);

-- DocumentLines
CREATE TABLE IF NOT EXISTS DocumentLines (
    Id                   UUID            NOT NULL PRIMARY KEY,
    ElectronicDocumentId UUID            NOT NULL REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
    Code                 VARCHAR(50)     NULL,
    Description          VARCHAR(500)    NOT NULL,
    Quantity             NUMERIC(18,6)   NOT NULL,
    UnitPrice            NUMERIC(18,6)   NOT NULL,
    Discount             NUMERIC(18,4)   NOT NULL,
    Subtotal             NUMERIC(18,4)   NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_documentlines_documentid ON DocumentLines(ElectronicDocumentId);

-- EmailLogs
CREATE TABLE IF NOT EXISTS EmailLogs (
    Id                   UUID            NOT NULL PRIMARY KEY,
    ElectronicDocumentId UUID            NOT NULL REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
    RecipientEmail       VARCHAR(255)    NOT NULL,
    Subject              VARCHAR(500)    NOT NULL,
    Status               INTEGER         NOT NULL,
    ErrorMessage         VARCHAR(2000)   NULL,
    SentAt               TIMESTAMPTZ     NULL,
    CreatedAt            TIMESTAMPTZ     NOT NULL,
    UpdatedAt            TIMESTAMPTZ     NULL
);
CREATE INDEX IF NOT EXISTS ix_emaillogs_documentid ON EmailLogs(ElectronicDocumentId);
CREATE INDEX IF NOT EXISTS ix_emaillogs_status     ON EmailLogs(Status);
CREATE INDEX IF NOT EXISTS ix_emaillogs_sentat     ON EmailLogs(SentAt);

-- SmtpConfigurations
CREATE TABLE IF NOT EXISTS SmtpConfigurations (
    Id                UUID            NOT NULL PRIMARY KEY,
    Name              VARCHAR(100)    NOT NULL,
    Host              VARCHAR(255)    NOT NULL,
    Port              INTEGER         NOT NULL,
    UseSsl            BOOLEAN         NOT NULL,
    Username          VARCHAR(255)    NOT NULL,
    EncryptedPassword TEXT            NOT NULL,
    FromEmail         VARCHAR(255)    NOT NULL,
    FromName          VARCHAR(100)    NOT NULL,
    IsActive          BOOLEAN         NOT NULL,
    CreatedAt         TIMESTAMPTZ     NOT NULL,
    UpdatedAt         TIMESTAMPTZ     NULL
);
