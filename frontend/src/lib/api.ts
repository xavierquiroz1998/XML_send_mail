import axios, { AxiosError } from 'axios'
import type {
  DocumentDto,
  EmailLogDto,
  ProblemDetails,
  SaveSmtpRequest,
  SmtpConfigurationDto,
  UploadResultDto,
} from './types'

// Vite proxy reenvía /api -> http://localhost:5099 en dev (ver vite.config.ts).
// En prod, Nginx hará el mismo redireccionamiento.
const client = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

// Hacemos los errores HTTP útiles: cualquier respuesta con status >= 400
// se convierte en un Error con .message tomado de ProblemDetails.detail|title
// y .problem (el ProblemDetails completo) para casos de inspección detallada.
client.interceptors.response.use(
  (r) => r,
  (error: AxiosError<ProblemDetails>) => {
    const data = error.response?.data
    const message =
      data?.detail ??
      data?.title ??
      error.message ??
      'Error desconocido'
    const enriched: Error & { problem?: ProblemDetails; status?: number } = new Error(message)
    enriched.problem = data
    enriched.status = error.response?.status
    return Promise.reject(enriched)
  }
)

export const documentsApi = {
  upload: async (files: File[]): Promise<UploadResultDto[]> => {
    const form = new FormData()
    for (const f of files) form.append('files', f, f.name)
    const { data } = await client.post<UploadResultDto[]>('/documents/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },

  list: async (skip = 0, take = 50): Promise<DocumentDto[]> => {
    const { data } = await client.get<DocumentDto[]>('/documents', {
      params: { skip, take },
    })
    return data
  },

  getById: async (id: string): Promise<DocumentDto> => {
    const { data } = await client.get<DocumentDto>(`/documents/${id}`)
    return data
  },

  ridePdfUrl: (id: string) => `/api/documents/${id}/ride`,

  send: async (id: string, recipientOverride?: string | null): Promise<EmailLogDto> => {
    const { data } = await client.post<EmailLogDto>(`/documents/${id}/send`, {
      recipientOverride: recipientOverride ?? null,
    })
    return data
  },

  listEmails: async (id: string): Promise<EmailLogDto[]> => {
    const { data } = await client.get<EmailLogDto[]>(`/documents/${id}/emails`)
    return data
  },
}

export const smtpApi = {
  list: async (): Promise<SmtpConfigurationDto[]> => {
    const { data } = await client.get<SmtpConfigurationDto[]>('/smtp-config')
    return data
  },

  create: async (body: SaveSmtpRequest): Promise<SmtpConfigurationDto> => {
    const { data } = await client.post<SmtpConfigurationDto>('/smtp-config', body)
    return data
  },

  update: async (id: string, body: SaveSmtpRequest): Promise<SmtpConfigurationDto> => {
    const { data } = await client.put<SmtpConfigurationDto>(`/smtp-config/${id}`, body)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await client.delete(`/smtp-config/${id}`)
  },
}

export const healthApi = {
  get: async () => {
    const { data } = await client.get('/health')
    return data
  },
}
