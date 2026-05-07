// Tipos espejo de los DTOs de la API (XmlEmailSender.API).

export interface DocumentLineDto {
  code: string;
  description: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  subtotal: number;
}

export interface TaxBucketDto {
  codigoPorcentaje: string;
  label: string;          // "IVA 15%", "IVA 0%", "Exento de IVA", etc.
  baseImponible: number;
  valor: number;
}

export interface DocumentDto {
  id: string;
  type: number;           // 1=Invoice, 2=CreditNote, 3=WithholdingReceipt
  typeName: string;
  accessKey: string;
  documentNumber: string;
  issueDate: string;      // ISO
  environment: string;
  issuerRuc: string;
  issuerBusinessName: string;
  receiverIdentification: string;
  receiverName: string;
  receiverEmail: string | null;
  subtotal: number;
  taxes: number;
  total: number;
  lines: DocumentLineDto[];
  taxBreakdown: TaxBucketDto[];
}

export interface UploadResultDto {
  fileName: string;
  success: boolean;
  document: DocumentDto | null;
  errorCode: string | null;
  errorMessage: string | null;
}

export interface EmailLogDto {
  id: string;
  electronicDocumentId: string;
  recipientEmail: string;
  subject: string;
  status: number;         // 0=Pending, 1=Sent, 2=Failed
  statusName: string;
  errorMessage: string | null;
  sentAt: string | null;
  createdAt: string;
}

export interface SmtpConfigurationDto {
  id: string;
  name: string;
  host: string;
  port: number;
  useSsl: boolean;
  username: string;
  fromEmail: string;
  fromName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface SaveSmtpRequest {
  name: string;
  host: string;
  port: number;
  useSsl: boolean;
  username: string;
  password: string | null;
  fromEmail: string;
  fromName: string;
  activate: boolean;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export const DocumentTypeNames: Record<number, string> = {
  1: 'Factura',
  2: 'Nota de Crédito',
  3: 'Retención',
};

export const EmailStatusNames: Record<number, string> = {
  0: 'Pendiente',
  1: 'Enviado',
  2: 'Fallido',
};
