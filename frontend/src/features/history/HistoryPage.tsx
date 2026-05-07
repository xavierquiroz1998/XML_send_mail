import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { Download, Send, Mail, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/Input'
import { documentsApi } from '@/lib/api'
import type { DocumentDto, EmailLogDto } from '@/lib/types'
import { DocumentTypeNames, EmailStatusNames } from '@/lib/types'

const statusBadgeVariant = (status: number) =>
  status === 1 ? 'success' : status === 2 ? 'error' : 'warning'

export function HistoryPage() {
  const [selected, setSelected] = useState<DocumentDto | null>(null)

  const docs = useQuery({
    queryKey: ['documents'],
    queryFn: () => documentsApi.list(0, 100),
  })

  return (
    <section>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Historial de comprobantes</h1>
          <p className="text-slate-600">
            Documentos procesados y su estado de envío.
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => docs.refetch()}
          disabled={docs.isFetching}
        >
          <RefreshCw size={14} className={docs.isFetching ? 'animate-spin' : ''} />
          Recargar
        </Button>
      </div>

      {docs.isLoading && <p className="text-slate-500">Cargando…</p>}
      {docs.isError && (
        <p className="text-red-600">No se pudo cargar el historial.</p>
      )}

      {docs.data && docs.data.length === 0 && (
        <div className="rounded-lg border border-dashed border-slate-300 bg-white p-10 text-center text-slate-500">
          No hay comprobantes todavía. Sube uno en la sección{' '}
          <strong>Subir XML</strong>.
        </div>
      )}

      {docs.data && docs.data.length > 0 && (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs font-medium text-slate-500">
              <tr>
                <th className="px-4 py-3">Tipo</th>
                <th className="px-4 py-3">Número</th>
                <th className="px-4 py-3">Emisor (RUC)</th>
                <th className="px-4 py-3">Receptor</th>
                <th className="px-4 py-3">Fecha</th>
                <th className="px-4 py-3 text-right">Total</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {docs.data.map((d) => (
                <tr key={d.id} className="hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <Badge variant="info">
                      {DocumentTypeNames[d.type] ?? d.typeName}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs">{d.documentNumber}</td>
                  <td className="px-4 py-3">
                    <div className="font-medium">{d.issuerBusinessName}</div>
                    <div className="text-xs text-slate-500">{d.issuerRuc}</div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium">{d.receiverName}</div>
                    <div className="text-xs text-slate-500">
                      {d.receiverEmail ?? 'sin correo'}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-slate-600">
                    {new Date(d.issueDate).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right font-medium">
                    {d.total.toFixed(2)}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setSelected(d)}
                    >
                      Ver
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <DocumentDetailModal
        document={selected}
        onClose={() => setSelected(null)}
      />
    </section>
  )
}

function DocumentDetailModal({
  document,
  onClose,
}: {
  document: DocumentDto | null
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [recipient, setRecipient] = useState('')

  const emails = useQuery({
    queryKey: ['emails', document?.id],
    queryFn: () => documentsApi.listEmails(document!.id),
    enabled: !!document,
  })

  const send = useMutation({
    mutationFn: (override: string | null) =>
      documentsApi.send(document!.id, override),
    onSuccess: () => {
      toast.success('Correo enviado')
      queryClient.invalidateQueries({ queryKey: ['emails', document?.id] })
    },
    onError: (err: Error) => toast.error(err.message),
  })

  if (!document) return null

  return (
    <Modal
      open={!!document}
      onClose={onClose}
      title={`${DocumentTypeNames[document.type] ?? document.typeName} ${document.documentNumber}`}
      description={`${document.issuerBusinessName} · ${document.environment}`}
      size="lg"
    >
      <div className="space-y-5">
        <div className="grid grid-cols-2 gap-4 text-sm">
          <Field label="RUC emisor" value={document.issuerRuc} />
          <Field label="Fecha de emisión" value={new Date(document.issueDate).toLocaleDateString()} />
          <Field label="Receptor" value={document.receiverName} />
          <Field label="Identificación" value={document.receiverIdentification} />
          <Field label="Correo del XML" value={document.receiverEmail ?? '—'} />
          <Field
            label="Clave de acceso"
            value={<code className="text-xs">{document.accessKey}</code>}
          />
        </div>

        {document.lines.length > 0 && (
          <div>
            <h4 className="mb-2 text-sm font-semibold">Detalle</h4>
            <table className="w-full border border-slate-200 text-sm">
              <thead className="bg-slate-50 text-xs text-slate-500">
                <tr>
                  <th className="px-3 py-2 text-left">Código</th>
                  <th className="px-3 py-2 text-left">Descripción</th>
                  <th className="px-3 py-2 text-right">Cant.</th>
                  <th className="px-3 py-2 text-right">P. Unit.</th>
                  <th className="px-3 py-2 text-right">Total</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {document.lines.map((l, i) => (
                  <tr key={i}>
                    <td className="px-3 py-2 font-mono text-xs">{l.code}</td>
                    <td className="px-3 py-2">{l.description}</td>
                    <td className="px-3 py-2 text-right">{l.quantity.toFixed(2)}</td>
                    <td className="px-3 py-2 text-right">{l.unitPrice.toFixed(2)}</td>
                    <td className="px-3 py-2 text-right">{l.subtotal.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="rounded-md bg-slate-50 p-4 text-sm">
          <div className="flex justify-between">
            <span>Subtotal</span>
            <span>USD {document.subtotal.toFixed(2)}</span>
          </div>
          {document.taxBreakdown.map((b) => (
            <div key={b.codigoPorcentaje} className="flex justify-between text-slate-600">
              <span>{b.label}</span>
              <span>USD {b.valor.toFixed(2)}</span>
            </div>
          ))}
          <div className="mt-2 flex justify-between border-t border-slate-200 pt-2 font-semibold">
            <span>Total</span>
            <span>USD {document.total.toFixed(2)}</span>
          </div>
        </div>

        <div className="flex flex-wrap items-end gap-2 border-t border-slate-200 pt-4">
          <div className="flex-1 min-w-[220px]">
            <label className="block text-xs font-medium text-slate-600 mb-1">
              Reenviar a (opcional)
            </label>
            <Input
              type="email"
              placeholder={document.receiverEmail ?? 'correo del cliente'}
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
            />
          </div>
          <Button
            disabled={send.isPending || (!recipient && !document.receiverEmail)}
            onClick={() => send.mutate(recipient || null)}
          >
            <Send size={14} />
            {send.isPending ? 'Enviando…' : 'Enviar correo'}
          </Button>
          <a
            href={documentsApi.ridePdfUrl(document.id)}
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center gap-2 h-10 px-4 text-sm rounded-md bg-white text-slate-900 border border-slate-300 hover:bg-slate-50"
          >
            <Download size={14} />
            Descargar RIDE
          </a>
        </div>

        <div>
          <h4 className="mb-2 text-sm font-semibold flex items-center gap-2">
            <Mail size={14} /> Historial de envíos
          </h4>
          {emails.isLoading && <p className="text-sm text-slate-500">Cargando…</p>}
          {emails.data && emails.data.length === 0 && (
            <p className="text-sm text-slate-500">Aún no se ha enviado este comprobante.</p>
          )}
          {emails.data && emails.data.length > 0 && (
            <ul className="space-y-2">
              {emails.data.map((e) => (
                <EmailLogRow key={e.id} log={e} />
              ))}
            </ul>
          )}
        </div>
      </div>
    </Modal>
  )
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <div className="text-xs uppercase tracking-wide text-slate-500">{label}</div>
      <div className="font-medium">{value}</div>
    </div>
  )
}

function EmailLogRow({ log }: { log: EmailLogDto }) {
  return (
    <li className="flex items-start justify-between rounded-md border border-slate-200 p-3 text-sm">
      <div>
        <div className="flex items-center gap-2">
          <Badge variant={statusBadgeVariant(log.status)}>
            {EmailStatusNames[log.status]}
          </Badge>
          <span className="font-medium">{log.recipientEmail}</span>
        </div>
        <div className="text-xs text-slate-500 mt-1">{log.subject}</div>
        {log.errorMessage && (
          <div className="text-xs text-red-600 mt-1">{log.errorMessage}</div>
        )}
      </div>
      <div className="text-xs text-slate-500 text-right whitespace-nowrap">
        {new Date(log.sentAt ?? log.createdAt).toLocaleString()}
      </div>
    </li>
  )
}
