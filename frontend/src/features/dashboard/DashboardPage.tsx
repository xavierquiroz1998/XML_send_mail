import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'

interface HealthResponse {
  status: string
  service: string
  timestamp: string
}

export function DashboardPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['health'],
    queryFn: async () => (await api.get<HealthResponse>('/health')).data,
  })

  return (
    <section>
      <h1 className="text-2xl font-semibold mb-2">Dashboard</h1>
      <p className="text-slate-600 mb-6">Resumen de envíos — gráficas en Fase 5.</p>

      <div className="rounded-lg border border-slate-200 bg-white p-6">
        <h2 className="font-medium mb-2">Estado del backend</h2>
        {isLoading && <p className="text-slate-500">Consultando…</p>}
        {isError && <p className="text-red-600">No se pudo conectar con la API.</p>}
        {data && (
          <pre className="text-sm bg-slate-50 p-3 rounded">
{JSON.stringify(data, null, 2)}
          </pre>
        )}
      </div>
    </section>
  )
}
