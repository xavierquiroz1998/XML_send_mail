-- ElectronicDocuments
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ElectronicDocuments')
BEGIN
    CREATE TABLE ElectronicDocuments (
        Id                    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [Type]                INT              NOT NULL,
        AccessKey             NVARCHAR(49)     NOT NULL,
        DocumentNumber        NVARCHAR(30)     NOT NULL,
        IssueDate             DATETIME2        NOT NULL,
        Environment           NVARCHAR(20)     NOT NULL,

        Issuer_Ruc            NVARCHAR(13)     NOT NULL,
        Issuer_BusinessName   NVARCHAR(300)    NOT NULL,
        Issuer_CommercialName NVARCHAR(300)    NULL,
        Issuer_Address        NVARCHAR(300)    NULL,

        Receiver_IdType       NVARCHAR(2)      NOT NULL,
        Receiver_Id           NVARCHAR(20)     NOT NULL,
        Receiver_Name         NVARCHAR(300)    NOT NULL,
        Receiver_Email        NVARCHAR(255)    NULL,
        Receiver_Phone        NVARCHAR(50)     NULL,
        Receiver_Address      NVARCHAR(300)    NULL,

        Subtotal              DECIMAL(18,4)    NOT NULL,
        Taxes                 DECIMAL(18,4)    NOT NULL,
        Total                 DECIMAL(18,4)    NOT NULL,
        OriginalXml           NVARCHAR(MAX)    NOT NULL,

        CreatedAt             DATETIME2        NOT NULL,
        UpdatedAt             DATETIME2        NULL
    );
    CREATE UNIQUE INDEX UX_ElectronicDocuments_AccessKey ON ElectronicDocuments(AccessKey);
    CREATE INDEX IX_ElectronicDocuments_IssueDate ON ElectronicDocuments(IssueDate);
END
GO

-- DocumentLines
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DocumentLines')
BEGIN
    CREATE TABLE DocumentLines (
        Id                   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ElectronicDocumentId UNIQUEIDENTIFIER NOT NULL
            FOREIGN KEY REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
        Code                 NVARCHAR(50)     NULL,
        [Description]        NVARCHAR(500)    NOT NULL,
        Quantity             DECIMAL(18,6)    NOT NULL,
        UnitPrice            DECIMAL(18,6)    NOT NULL,
        Discount             DECIMAL(18,4)    NOT NULL,
        Subtotal             DECIMAL(18,4)    NOT NULL
    );
    CREATE INDEX IX_DocumentLines_DocumentId ON DocumentLines(ElectronicDocumentId);
END
GO

-- EmailLogs
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailLogs')
BEGIN
    CREATE TABLE EmailLogs (
        Id                   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ElectronicDocumentId UNIQUEIDENTIFIER NOT NULL
            FOREIGN KEY REFERENCES ElectronicDocuments(Id) ON DELETE CASCADE,
        RecipientEmail       NVARCHAR(255)    NOT NULL,
        Subject              NVARCHAR(500)    NOT NULL,
        Status               INT              NOT NULL,
        ErrorMessage         NVARCHAR(2000)   NULL,
        SentAt               DATETIME2        NULL,
        CreatedAt            DATETIME2        NOT NULL,
        UpdatedAt            DATETIME2        NULL
    );
    CREATE INDEX IX_EmailLogs_DocumentId ON EmailLogs(ElectronicDocumentId);
    CREATE INDEX IX_EmailLogs_Status     ON EmailLogs(Status);
    CREATE INDEX IX_EmailLogs_SentAt     ON EmailLogs(SentAt);
END
GO

-- SmtpConfigurations
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SmtpConfigurations')
BEGIN
    CREATE TABLE SmtpConfigurations (
        Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name              NVARCHAR(100)    NOT NULL,
        Host              NVARCHAR(255)    NOT NULL,
        Port              INT              NOT NULL,
        UseSsl            BIT              NOT NULL,
        Username          NVARCHAR(255)    NOT NULL,
        EncryptedPassword NVARCHAR(MAX)    NOT NULL,
        FromEmail         NVARCHAR(255)    NOT NULL,
        FromName          NVARCHAR(100)    NOT NULL,
        IsActive          BIT              NOT NULL,
        CreatedAt         DATETIME2        NOT NULL,
        UpdatedAt         DATETIME2        NULL
    );
END
GO
