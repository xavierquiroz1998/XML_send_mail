import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { Plus, Trash2, Edit, CheckCircle2 } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Card } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { Modal } from '@/components/ui/Modal'
import { smtpApi } from '@/lib/api'
import type { SmtpConfigurationDto, SaveSmtpRequest } from '@/lib/types'

const emptyForm: SaveSmtpRequest = {
  name: '',
  host: '',
  port: 587,
  useSsl: true,
  username: '',
  password: '',
  fromEmail: '',
  fromName: '',
  activate: true,
}

export function SettingsPage() {
  const [editing, setEditing] = useState<SmtpConfigurationDto | null>(null)
  const [creating, setCreating] = useState(false)

  const list = useQuery({ queryKey: ['smtp'], queryFn: smtpApi.list })

  return (
    <section>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Configuración SMTP</h1>
          <p className="text-slate-600">
            Define las credenciales para enviar correos. Solo una puede estar activa a la vez.
          </p>
        </div>
        <Button onClick={() => setCreating(true)}>
          <Plus size={14} /> Nueva configuración
        </Button>
      </div>

      {list.isLoading && <p className="text-slate-500">Cargando…</p>}
      {list.isError && <p className="text-red-600">No se pudo cargar la configuración.</p>}

      {list.data && list.data.length === 0 && (
        <div className="rounded-lg border border-dashed border-slate-300 bg-white p-10 text-center text-slate-500">
          No hay configuraciones SMTP todavía. Crea una para poder enviar correos.
        </div>
      )}

      <div className="grid gap-3">
        {list.data?.map((c) => (
          <SmtpCard key={c.id} config={c} onEdit={() => setEditing(c)} />
        ))}
      </div>

      {creating && (
        <SmtpFormModal
          initial={null}
          onClose={() => setCreating(false)}
        />
      )}
      {editing && (
        <SmtpFormModal
          initial={editing}
          onClose={() => setEditing(null)}
        />
      )}
    </section>
  )
}

function SmtpCard({
  config,
  onEdit,
}: {
  config: SmtpConfigurationDto
  onEdit: () => void
}) {
  const queryClient = useQueryClient()
  const del = useMutation({
    mutationFn: () => smtpApi.delete(config.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smtp'] })
      toast.success('Configuración eliminada')
    },
    onError: (err: Error) => toast.error(err.message),
  })

  return (
    <Card className={config.isActive ? 'border-l-4 border-l-green-500' : ''}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h3 className="font-semibold">{config.name}</h3>
            {config.isActive && (
              <Badge variant="success">
                <CheckCircle2 size={12} className="mr-1" />
                Activa
              </Badge>
            )}
          </div>
          <p className="text-sm text-slate-600 mt-1">
            <code>{config.host}:{config.port}</code> · {config.useSsl ? 'SSL' : 'sin SSL'} ·{' '}
            <span className="text-slate-500">{config.username}</span>
          </p>
          <p className="text-xs text-slate-500 mt-1">
            Remitente: {config.fromName} &lt;{config.fromEmail}&gt;
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" onClick={onEdit}>
            <Edit size={14} /> Editar
          </Button>
          <Button
            variant="danger"
            size="sm"
            onClick={() => {
              if (confirm(`¿Eliminar "${config.name}"?`)) del.mutate()
            }}
            disabled={del.isPending}
          >
            <Trash2 size={14} />
          </Button>
        </div>
      </div>
    </Card>
  )
}

function SmtpFormModal({
  initial,
  onClose,
}: {
  initial: SmtpConfigurationDto | null
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const isEdit = !!initial

  const [form, setForm] = useState<SaveSmtpRequest>(
    initial
      ? {
          name: initial.name,
          host: initial.host,
          port: initial.port,
          useSsl: initial.useSsl,
          username: initial.username,
          password: '', // vacío al editar = mantener
          fromEmail: initial.fromEmail,
          fromName: initial.fromName,
          activate: initial.isActive,
        }
      : emptyForm
  )

  const save = useMutation({
    mutationFn: (body: SaveSmtpRequest) =>
      isEdit ? smtpApi.update(initial!.id, body) : smtpApi.create(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smtp'] })
      toast.success(isEdit ? 'Configuración actualizada' : 'Configuración creada')
      onClose()
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const submit = (e: React.FormEvent) => {
    e.preventDefault()
    // Al editar, si la password está vacía, mandamos null para que el backend
    // mantenga la actual.
    const body: SaveSmtpRequest = {
      ...form,
      password: form.password || null,
    }
    save.mutate(body)
  }

  return (
    <Modal
      open={true}
      onClose={onClose}
      title={isEdit ? 'Editar configuración SMTP' : 'Nueva configuración SMTP'}
      description={
        isEdit
          ? 'Deja la contraseña vacía para mantener la actual.'
          : 'La contraseña se cifra antes de almacenarse.'
      }
    >
      <form onSubmit={submit} className="space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <FormField label="Nombre" required>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder="Gmail principal"
              required
            />
          </FormField>
          <FormField label="Host" required>
            <Input
              value={form.host}
              onChange={(e) => setForm({ ...form, host: e.target.value })}
              placeholder="smtp.gmail.com"
              required
            />
          </FormField>
          <FormField label="Puerto" required>
            <Input
              type="number"
              value={form.port}
              onChange={(e) => setForm({ ...form, port: Number(e.target.value) })}
              required
            />
          </FormField>
          <FormField label="Usar SSL/TLS">
            <label className="flex items-center gap-2 mt-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={form.useSsl}
                onChange={(e) => setForm({ ...form, useSsl: e.target.checked })}
                className="h-4 w-4"
              />
              Activar (587 STARTTLS, 465 SSL)
            </label>
          </FormField>
          <FormField label="Usuario" required>
            <Input
              value={form.username}
              onChange={(e) => setForm({ ...form, username: e.target.value })}
              placeholder="usuario@dominio.com"
              required
            />
          </FormField>
          <FormField label={isEdit ? 'Nueva contraseña (opcional)' : 'Contraseña'} required={!isEdit}>
            <Input
              type="password"
              value={form.password ?? ''}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              placeholder={isEdit ? '(sin cambios)' : ''}
              required={!isEdit}
            />
          </FormField>
          <FormField label="From Email" required>
            <Input
              type="email"
              value={form.fromEmail}
              onChange={(e) => setForm({ ...form, fromEmail: e.target.value })}
              required
            />
          </FormField>
          <FormField label="From Name" required>
            <Input
              value={form.fromName}
              onChange={(e) => setForm({ ...form, fromName: e.target.value })}
              required
            />
          </FormField>
        </div>

        <FormField label="Activar esta configuración">
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={form.activate}
              onChange={(e) => setForm({ ...form, activate: e.target.checked })}
              className="h-4 w-4"
            />
            Marcar como activa (desactivará otras)
          </label>
        </FormField>

        <div className="flex justify-end gap-2 pt-2 border-t border-slate-200">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={save.isPending}>
            {save.isPending ? 'Guardando…' : isEdit ? 'Actualizar' : 'Crear'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

function FormField({
  label,
  required,
  children,
}: {
  label: string
  required?: boolean
  children: React.ReactNode
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-slate-600 mb-1">
        {label} {required && <span className="text-red-500">*</span>}
      </label>
      {children}
    </div>
  )
}
