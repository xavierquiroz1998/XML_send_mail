import { Routes, Route } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { UploadPage } from '@/features/upload/UploadPage'
import { HistoryPage } from '@/features/history/HistoryPage'
import { SettingsPage } from '@/features/settings/SettingsPage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<UploadPage />} />
        <Route path="historial" element={<HistoryPage />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="configuracion" element={<SettingsPage />} />
      </Route>
    </Routes>
  )
}

export default App
