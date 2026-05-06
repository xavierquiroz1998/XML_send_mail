import axios from 'axios'

// Vite proxy reenvía /api -> http://localhost:5099 en dev (ver vite.config.ts).
// En prod, Nginx hará el mismo redireccionamiento.
export const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
    // Punto único para mapear errores HTTP a notificaciones más adelante.
    return Promise.reject(error)
  }
)
