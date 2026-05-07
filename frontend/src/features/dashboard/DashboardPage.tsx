import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
  Legend,
} from 'recharts'
import { FileText, Receipt, FileMinus, DollarSign } from 'lucide-react'
import { Card } from '@/components/ui/Card'
import { documentsApi } from '@/lib/api'
import type { DocumentDto } from '@/lib/types'

export function DashboardPage() {
  const docs = useQuery({
    queryKey: ['documents'],
    queryFn: () => documentsApi.list(0, 200),
  })

  const stats = useMemo(() => computeStats(docs.data ?? []), [docs.data])

  return (
    <section>
      <h1 className="text-2xl font-semibold mb-2">Dashboard</h1>
      <p className="text-slate-600 mb-6">
        Resumen de comprobantes procesados.
      </p>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <StatCard
          icon={<FileText size={20} />}
          label="Total"
          value={stats.total}
          color="bg-slate-900"
        />
        <StatCard
          icon={<Receipt size={20} />}
          label="Facturas"
          value={stats.invoices}
          color="bg-blue-600"
        />
        <StatCard
          icon={<FileMinus size={20} />}
          label="Notas de crédito"
          value={stats.creditNotes}
          color="bg-amber-600"
        />
        <StatCard
          icon={<DollarSign size={20} />}
          label="Total facturado"
          value={`USD ${stats.totalAmount.toFixed(2)}`}
          color="bg-green-600"
        />
      </div>

      <Card>
        <h3 className="text-lg font-semibold mb-1">Comprobantes por día</h3>
        <p className="text-sm text-slate-500 mb-4">
          Últimos 14 días con actividad.
        </p>
        {stats.daily.length === 0 ? (
          <p className="text-sm text-slate-500 py-10 text-center">
            Sube comprobantes para ver la gráfica.
          </p>
        ) : (
          <div style={{ width: '100%', height: 280 }}>
            <ResponsiveContainer>
              <BarChart data={stats.daily}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
                <Tooltip />
                <Legend />
                <Bar dataKey="invoices" stackId="a" fill="#2563eb" name="Facturas" />
                <Bar dataKey="creditNotes" stackId="a" fill="#d97706" name="Notas de crédito" />
                <Bar dataKey="withholdings" stackId="a" fill="#059669" name="Retenciones" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </Card>
    </section>
  )
}

function StatCard({
  icon,
  label,
  value,
  color,
}: {
  icon: React.ReactNode
  label: string
  value: string | number
  color: string
}) {
  return (
    <Card className="flex items-center gap-4">
      <div className={`flex h-10 w-10 items-center justify-center rounded-md ${color} text-white`}>
        {icon}
      </div>
      <div>
        <div className="text-xs uppercase tracking-wide text-slate-500">{label}</div>
        <div className="text-xl font-semibold">{value}</div>
      </div>
    </Card>
  )
}

interface DailyBucket {
  date: string
  invoices: number
  creditNotes: number
  withholdings: number
}

function computeStats(docs: DocumentDto[]) {
  let invoices = 0
  let creditNotes = 0
  let withholdings = 0
  let totalAmount = 0
  const byDay = new Map<string, DailyBucket>()

  for (const d of docs) {
    if (d.type === 1) invoices++
    else if (d.type === 2) creditNotes++
    else if (d.type === 3) withholdings++
    totalAmount += d.total

    const day = d.issueDate.slice(0, 10) // yyyy-MM-dd
    let bucket = byDay.get(day)
    if (!bucket) {
      bucket = { date: day, invoices: 0, creditNotes: 0, withholdings: 0 }
      byDay.set(day, bucket)
    }
    if (d.type === 1) bucket.invoices++
    else if (d.type === 2) bucket.creditNotes++
    else if (d.type === 3) bucket.withholdings++
  }

  const daily = Array.from(byDay.values())
    .sort((a, b) => a.date.localeCompare(b.date))
    .slice(-14)

  return {
    total: docs.length,
    invoices,
    creditNotes,
    withholdings,
    totalAmount,
    daily,
  }
}
