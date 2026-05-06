export function UploadPage() {
  return (
    <section>
      <h1 className="text-2xl font-semibold mb-2">Subir comprobantes XML</h1>
      <p className="text-slate-600 mb-6">
        Carga uno o varios archivos XML autorizados por el SRI para parsearlos,
        generar el RIDE y enviarlos por correo al receptor.
      </p>
      <div className="rounded-lg border-2 border-dashed border-slate-300 bg-white p-12 text-center text-slate-500">
        Dropzone — implementación en Fase 5
      </div>
    </section>
  )
}
