import { useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { FileUp, FileText, CheckCircle2, XCircle, Send, Download, Mail } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Badge } from '@/components/ui/Badge'
import { Card } from '@/components/ui/Card'
import { Input } from '@/components/ui/Input'
import { documentsApi } from '@/lib/api'
import type { UploadResultDto } from '@/lib/types'
import { DocumentTypeNames } from '@/lib/types'

export function UploadPage() {
  const [results, setResults] = useState<UploadResultDto[]>([])
  const queryClient = useQueryClient()

  const upload = useMutation({
    mutationFn: documentsApi.upload,
    onSuccess: (data) => {
      setResults((prev) => [...data, ...prev])
      queryClient.invalidateQueries({ queryKey: ['documents'] })
      const ok = data.filter((r) => r.success).length
      const fail = data.length - ok
      if (fail === 0) toast.success(`${ok} XML procesado(s) correctamente`)
      else toast.error(`${ok} OK · ${fail} con error`)
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: { 'application/xml': ['.xml'], 'text/xml': ['.xml'] },
    multiple: true,
    onDrop: (files) => {
      if (files.length > 0) upload.mutate(files)
    },
  })

  return (
    <section>
      <h1 className="text-2xl font-semibold mb-2">Subir comprobantes XML</h1>
      <p className="text-slate-600 mb-6">
        Arrastra uno o varios archivos XML autorizados por el SRI, o haz clic para seleccionarlos.
      </p>

      <div
        {...getRootProps()}
        className={`mb-6 cursor-pointer rounded-lg border-2 border-dashed p-12 text-center transition-colors ${
          isDragActive
            ? 'border-slate-900 bg-slate-50'
            : 'border-slate-300 bg-white hover:border-slate-400'
        }`}
      >
        <input {...getInputProps()} />
        <FileUp className="mx-auto mb-3 text-slate-400" size={40} />
        {upload.isPending ? (
          <p className="text-slate-700">Procesando…</p>
        ) : isDragActive ? (
          <p className="text-slate-900 font-medium">Suelta los archivos aquí</p>
        ) : (
          <>
            <p className="text-slate-700 font-medium">
              Arrastra archivos XML o haz clic para seleccionar
            </p>
            <p className="mt-1 text-sm text-slate-500">
              Acepta múltiples archivos · facturas, notas de crédito, retenciones
            </p>
          </>
        )}
      </div>

      {results.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-lg font-semibold">Resultados ({results.length})</h2>
          {results.map((r, idx) => (
            <ResultCard key={idx} result={r} />
          ))}
        </div>
      )}
    </section>
  )
}

function ResultCard({ result }: { result: UploadResultDto }) {
  const [recipient, setRecipient] = useState('')

  const send = useMutation({
    mutationFn: (override: string | null) =>
      documentsApi.send(result.document!.id, override),
    onSuccess: (log) => {
      toast.success(`Correo enviado a ${log.recipientEmail}`)
    },
    onError: (err: Error) => toast.error(err.message),
  })

  if (!result.success) {
    return (
      <Card className="border-l-4 border-l-red-500">
        <div className="flex items-start gap-3">
          <XCircle className="mt-0.5 text-red-500 shrink-0" size={20} />
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <span className="font-medium">{result.fileName}</span>
              <Badge variant="error">Error</Badge>
            </div>
            <p className="mt-1 text-sm text-slate-600">
              <code className="text-xs bg-slate-100 px-1 py-0.5 rounded">{result.errorCode}</code>{' '}
              · {result.errorMessage}
            </p>
          </div>
        </div>
      </Card>
    )
  }

  const doc = result.document!
  const defaultEmail = doc.receiverEmail ?? ''

  return (
    <Card className="border-l-4 border-l-green-500">
      <div className="flex items-start gap-3">
        <CheckCircle2 className="mt-0.5 text-green-600 shrink-0" size={20} />
        <div className="flex-1 space-y-3">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <FileText size={16} className="text-slate-400" />
              <span className="font-medium">{result.fileName}</span>
              <Badge variant="success">Procesado</Badge>
              <Badge variant="info">{DocumentTypeNames[doc.type] ?? doc.typeName}</Badge>
            </div>
            <p className="mt-1 text-sm text-slate-600">
              {doc.documentNumber} · {doc.issuerBusinessName} · USD {doc.total.toFixed(2)}
            </p>
            <p className="text-xs text-slate-500">
              Receptor: {doc.receiverName} ({doc.receiverIdentification})
              {doc.receiverEmail && <> · {doc.receiverEmail}</>}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2 pt-2 border-t border-slate-100">
            <Input
              type="email"
              placeholder={defaultEmail || 'correo del cliente'}
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
              className="max-w-xs"
            />
            <Button
              size="sm"
              variant="primary"
              disabled={send.isPending || (!recipient && !defaultEmail)}
              onClick={() => send.mutate(recipient || null)}
            >
              <Send size={14} />
              {send.isPending ? 'Enviando…' : 'Enviar correo'}
            </Button>
            <a
              href={documentsApi.ridePdfUrl(doc.id)}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-2 h-8 px-3 text-sm rounded-md bg-white text-slate-900 border border-slate-300 hover:bg-slate-50"
            >
              <Download size={14} />
              Ver RIDE
            </a>
            {!doc.receiverEmail && !recipient && (
              <span className="text-xs text-amber-700 inline-flex items-center gap-1">
                <Mail size={12} />
                XML sin correo: ingrésalo arriba
              </span>
            )}
          </div>
        </div>
      </div>
    </Card>
  )
}
