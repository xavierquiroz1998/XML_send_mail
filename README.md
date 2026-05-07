# XmlEmailSender

Sistema para procesar comprobantes electrónicos del **SRI de Ecuador** (facturas, notas de crédito y comprobantes de retención): parsea el XML, genera el PDF RIDE y envía el comprobante por correo al receptor, manteniendo un historial de envíos.

> Estado actual: **Fase 2 / 6 completada**. La base (Domain, parser SRI, persistencia Dapper, API mínima, frontend scaffold) está funcional. Las fases 3-6 (RIDE PDF, CQRS + envío de correo, controllers REST + UI completa, deploy Linux) están en hoja de ruta.

---

## Tabla de contenidos

- [¿Qué hace el sistema?](#qué-hace-el-sistema)
- [Arquitectura](#arquitectura)
- [Stack técnico](#stack-técnico)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Requisitos previos](#requisitos-previos)
- [Instalación](#instalación)
- [Cómo correr el proyecto](#cómo-correr-el-proyecto)
- [Tests](#tests)
- [Configuración](#configuración)
- [Hoja de ruta (fases)](#hoja-de-ruta-fases)
- [Troubleshooting](#troubleshooting)

---

## ¿Qué hace el sistema?

El SRI de Ecuador autoriza comprobantes electrónicos (facturas, notas de crédito, retenciones) y los devuelve como XML. Las empresas necesitan enviar a sus clientes tanto el XML como una **representación impresa (RIDE)** en PDF.

**XmlEmailSender** automatiza ese flujo:

1. **Subir XML(s)** desde la interfaz web (uno o varios).
2. **Parsear** el XML — soporta tanto el sobre completo de `<autorizacion>` con CDATA como el comprobante directo.
3. **Extraer** automáticamente: número de comprobante, clave de acceso (validada con módulo 11), RUC del emisor, datos del receptor (nombre, identificación, correo si está en `<infoAdicional>`), fecha, totales y líneas.
4. **Generar el RIDE** (PDF) según el tipo de comprobante.
5. **Enviar por correo** el XML + PDF al receptor vía SMTP.
6. **Registrar** cada envío (estado, destinatario, fecha, error si lo hubo).
7. **Permitir reenvíos** sin necesidad del archivo original (el XML se guarda en la base).
8. **Configurar** las credenciales SMTP (Gmail, Outlook, etc.) desde la propia UI.

---

## Arquitectura

Clean Architecture con cuatro capas y dependencias siempre apuntando hacia adentro.

```
┌─────────────────────────────────────────────────────┐
│                       API                           │  Controllers, middleware,
│            (ASP.NET Core, Serilog)                  │  Program.cs, CORS, Swagger
└────────────────┬────────────────────────────────────┘
                 │
       ┌─────────┴─────────┐
       │                   │
┌──────▼──────┐    ┌───────▼─────────────────────────┐
│ Application │    │       Infrastructure            │  Dapper, MailKit,
│  (CQRS,     │    │  (parser SRI, repos Dapper,     │  QuestPDF, parser XML,
│  MediatR,   │    │   schema runner, type handlers) │  schema scripts SQL
│  validators)│    └───────┬─────────────────────────┘
└──────┬──────┘            │
       │                   │
       └─────┬─────────────┘
             │
       ┌─────▼─────┐
       │  Domain   │   Entidades, ValueObjects, interfaces de repos.
       │           │   Sin dependencias externas (puro C#).
       └───────────┘
```

**Principios aplicados:**

- **Domain puro**: cero dependencias de frameworks. `AccessKey` es un ValueObject que valida el dígito verificador (módulo 11) en su factory.
- **Result Pattern** en lugar de excepciones para flujos de negocio (`Result<T>` + `Error` con código y tipo).
- **Repository Pattern**: interfaces en `Domain/Repositories`, implementadas con Dapper en `Infrastructure/Persistence/Repositories`.
- **Strategy Pattern** para parsear cada tipo de comprobante (factura / nota de crédito / retención).
- **Unit of Work** explícito (Dapper no hace tracking; las transacciones son a mano).
- **Mini schema runner**: scripts SQL embedidos como recursos `.sqlite.sql` y `.postgres.sql`, aplicados al arrancar y registrados en `__schema_migrations`.

---

## Stack técnico

### Backend (.NET 8)

| Categoría | Librería |
|---|---|
| Web framework | ASP.NET Core 8 (Controllers) |
| CQRS / MediatR | `MediatR` 12 |
| Validación | `FluentValidation` 11 |
| Mapping | `AutoMapper` 13 |
| Acceso a datos | **`Dapper`** + `Microsoft.Data.Sqlite` / `Microsoft.Data.SqlClient` *(no EF Core)* |
| Logs | `Serilog` (Console + File) |
| Email | `MailKit` |
| PDF + QR | `QuestPDF`, `ZXing.Net`, `SkiaSharp` |
| Cifrado de password SMTP | `Microsoft.AspNetCore.DataProtection` |
| Tests | `xUnit`, `FluentAssertions`, `Moq` |

### Frontend (Vite + React 18)

| Categoría | Librería |
|---|---|
| Build | Vite |
| UI | React 18 + TypeScript |
| Estilos | Tailwind CSS 3 + shadcn/ui (a inicializar) |
| Estado servidor | TanStack Query v5 |
| Routing | React Router v6 |
| HTTP client | Axios |
| Drag & drop | react-dropzone |
| Toasts | react-hot-toast |
| Charts | Recharts |
| Iconos | lucide-react |

### Infraestructura

- **Dev**: SQLite (archivo local).
- **Prod / dev avanzado**: Postgres (corre en Docker). Ambos providers comparten interfaz (`IDbConnectionFactory`); el provider se elige por configuración.
- **Deploy futuro** (Fase 6): Ubuntu 22.04 + Nginx (reverse proxy) + Let's Encrypt + systemd.

---

## Estructura del repositorio

```
XmlEmailSender/
├── XmlEmailSender.sln
├── README.md
├── .gitignore
├── src/
│   ├── XmlEmailSender.Domain/          ← Entidades, VOs, Result/Error, interfaces de repos
│   │   ├── Common/                       Result.cs, Error.cs, Entity.cs
│   │   ├── Documents/                    AccessKey, ElectronicDocument, Issuer, Receiver, DocumentLine
│   │   ├── Emails/                       EmailLog, SmtpConfiguration, EmailStatus
│   │   └── Repositories/                 IElectronicDocumentRepository, IUnitOfWork, etc.
│   │
│   ├── XmlEmailSender.Application/     ← CQRS handlers, validators, abstracciones
│   │   ├── Abstractions/Parsing/         IXmlDocumentParser
│   │   └── DependencyInjection.cs
│   │
│   ├── XmlEmailSender.Infrastructure/  ← Implementaciones (Dapper, parser SRI, etc.)
│   │   ├── Parsing/
│   │   │   ├── SriXmlDocumentParser.cs   Orquestador (Strategy pattern)
│   │   │   ├── XmlExtractor.cs           Desempaqueta CDATA del sobre <autorizacion>
│   │   │   ├── XmlParsingHelpers.cs      ParseDecimal, ParseDate, FindCampoAdicional
│   │   │   └── Strategies/
│   │   │       ├── InvoiceXmlParser.cs
│   │   │       ├── CreditNoteXmlParser.cs
│   │   │       └── WithholdingXmlParser.cs
│   │   └── Persistence/
│   │       ├── DbConnectionFactory.cs    SQLite o Postgres según config
│   │       ├── UnitOfWork.cs             Conexión + transacción explícita (Dapper)
│   │       ├── Repositories/             Repos Dapper con SQL escrito a mano
│   │       ├── TypeHandlers/             Guid/DateTime ↔ TEXT para SQLite
│   │       └── Schema/
│   │           ├── SchemaMigrationRunner.cs
│   │           └── Scripts/
│   │               ├── 001_initial.sqlite.sql
│   │               └── 001_initial.postgres.sql
│   │
│   └── XmlEmailSender.API/             ← ASP.NET Core entry point
│       ├── Program.cs                    Serilog, CORS, DI, auto-migrate
│       ├── Controllers/HealthController.cs
│       └── appsettings.json              Database:Provider, ConnectionStrings:Default
│
├── tests/
│   ├── XmlEmailSender.Domain.Tests/        AccessKey + Result tests
│   ├── XmlEmailSender.Application.Tests/   (placeholder)
│   └── XmlEmailSender.Infrastructure.Tests/
│       ├── Parsing/                        Parser SRI (envelope, edge cases)
│       └── Persistence/                    End-to-end roundtrip contra SQLite real
│
└── frontend/                            ← Vite + React + TS + Tailwind
    ├── src/
    │   ├── components/layout/AppLayout.tsx
    │   ├── features/
    │   │   ├── upload/                    Pantalla de subida de XMLs
    │   │   ├── history/                   Historial de envíos
    │   │   ├── settings/                  Configuración SMTP
    │   │   └── dashboard/                 Métricas y health check
    │   ├── lib/
    │   │   ├── api.ts                     Axios con baseURL /api (proxy a la API .NET)
    │   │   ├── queryClient.ts             React Query config
    │   │   └── utils.ts                   cn() helper para shadcn
    │   ├── App.tsx                        Rutas
    │   └── main.tsx                       Providers (Router, QueryClient, Toaster)
    ├── tailwind.config.js
    └── vite.config.ts                     Proxy /api → http://localhost:5099
```

---

## Requisitos previos

| Herramienta | Versión mínima | Cómo verificar |
|---|---|---|
| .NET SDK | **8.0** (probado con 9.0.308) | `dotnet --version` |
| Node.js | **18 LTS** o superior (probado con 24.x) | `node --version` |
| npm | **9** o superior (probado con 11.x) | `npm --version` |
| Git | cualquier versión moderna | `git --version` |

**Opcional:**

- **DB Browser for SQLite** o **DBeaver** para inspeccionar la base de datos local (`xmlemailsender.db`).
- **Postman** o **REST Client** (extensión de VS Code) para probar la API; alternativamente usa el archivo `XmlEmailSender.API.http` que viene incluido.
- **Docker** (con Docker Compose) si vas a usar Postgres durante el desarrollo (ver sección "Levantar Postgres con Docker").

---

## Instalación

### 1. Clonar el repositorio

```powershell
git clone https://github.com/xavierquiroz1998/XML_send_mail.git
cd XML_send_mail
```

> **Si estás en una red corporativa con proxy SSL** y `git clone` falla con `self-signed certificate in certificate chain`, configura el repo para usar el cert store de Windows en lugar del bundle de OpenSSL:
> ```powershell
> git config --local http.sslBackend schannel
> ```
> No desactives `http.sslVerify` globalmente — eso te deja vulnerable a MITM en todos tus repos.

### 2. Restaurar dependencias del backend

```powershell
dotnet restore
dotnet build
```

Esto restaura todos los paquetes NuGet (Dapper, MailKit, QuestPDF, MediatR, Serilog, etc.) y compila las 7 proyectos. Debería terminar con `Build succeeded` y 0 errores.

### 3. Restaurar dependencias del frontend

```powershell
cd frontend
npm install
cd ..
```

### 4. (Opcional) Inicializar shadcn/ui

shadcn/ui es interactivo, así que se ejecuta a mano cuando vayas a empezar a usar componentes pre-hechos (Fase 5).

```powershell
cd frontend
npx shadcn@latest init
# Style: Default | Base color: Slate | CSS variables: Yes
npx shadcn@latest add button input card table dialog form label toast
cd ..
```

### 5. Verificar que la base de datos se crea sola

No hay que correr `dotnet ef database update` ni nada parecido — el sistema usa **Dapper + un schema runner propio** que aplica los scripts `.sql` automáticamente al arrancar la API.

Por defecto, la primera ejecución crea un archivo **SQLite** local (`xmlemailsender.db`) — no hace falta instalar ninguna base de datos. Si prefieres correr **Postgres** en Docker para tener un entorno más cercano a producción, ve a la siguiente sección.

### 6. (Opcional) Levantar Postgres con Docker

Si quieres usar Postgres durante el desarrollo, hay un `docker-compose.yml` listo (imagen `postgres:16-alpine`, ~150 MB):

```powershell
# Crear el archivo de variables locales (no se sube al repo)
copy .env.example .env

# Levantar Postgres en background
docker compose up -d db

# Ver el estado del healthcheck (queda "healthy" en ~10 s)
docker compose ps

# Apagar el contenedor manteniendo los datos
docker compose stop db

# Apagar y borrar TODO (datos incluidos)
docker compose down -v
```

El contenedor expone Postgres en **`localhost:5433`** (mapeado al `5432` del contenedor) con usuario `xmlemailsender`, base `xmlemailsender` y la password definida en `.env` (default `Dev_Passw0rd!`). Los datos persisten en el volumen `pg_data`.

> Usamos el puerto **5433** en el host para no chocar con un Postgres nativo que ya tengas instalado como servicio de Windows. Si quieres el puerto estándar `5432`, edita el mapeo en `docker-compose.yml` y la connection string en `appsettings.Docker.json`.

Para que la API use Postgres en lugar de SQLite, arráncala con el ambiente `Docker`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Docker"
dotnet run --project src/XmlEmailSender.API
```

Esto activa [`appsettings.Docker.json`](src/XmlEmailSender.API/appsettings.Docker.json), que sobrescribe el provider y la connection string. El schema runner detecta el provider y aplica los scripts `*.postgres.sql` correspondientes (`001_initial.postgres.sql`, `002_tax_buckets.postgres.sql`).

> **Nota:** los tests unitarios e integración siguen usando SQLite en archivo temporal (más rápido, sin Docker). Solo el runtime de dev/prod aprovecha Postgres.

---

## Cómo correr el proyecto

Necesitas **dos terminales**.

### Terminal 1: API .NET

```powershell
dotnet run --project src/XmlEmailSender.API
```

Salida esperada (la primera vez):

```
[INF] Aplicando migración: 001_initial.sqlite.sql
[INF] Migración aplicada: 001_initial.sqlite.sql
[INF] Iniciando XmlEmailSender API
[INF] Now listening on: http://localhost:5099
[INF] Application started. Press Ctrl+C to shut down.
```

> El puerto por defecto es **5099** (configurado en [vite.config.ts](frontend/vite.config.ts) como destino del proxy).
> Si necesitas cambiarlo, también actualiza el campo `proxy['/api']` del Vite config.

Verifica que arrancó correctamente:

```powershell
curl http://localhost:5099/api/health
# {"status":"ok","service":"XmlEmailSender","timestamp":"2026-..."}
```

Y se habrá creado el archivo SQLite `src/XmlEmailSender.API/xmlemailsender.db` con las tablas:
- `ElectronicDocuments`
- `DocumentLines`
- `DocumentTaxBuckets` *(desglose por código de IVA)*
- `EmailLogs`
- `SmtpConfigurations`
- `__schema_migrations` *(metadatos del runner)*

### Terminal 2: Frontend React

```powershell
cd frontend
npm run dev
```

Salida esperada:

```
  VITE v8.x.x  ready in xxx ms
  ➜  Local:   http://localhost:5173/
```

Abre [http://localhost:5173](http://localhost:5173) en el navegador. Verás el layout con cuatro secciones (Subir XML, Historial, Dashboard, Configuración). El Dashboard hace un fetch real al backend vía el proxy `/api/health` y muestra el JSON de respuesta — si lo ves, la conexión front ↔ back funciona.

### Swagger (documentación interactiva de la API)

Mientras la API esté corriendo en modo Development:

[http://localhost:5099/swagger](http://localhost:5099/swagger)

---

## Tests

Suite completa (Domain + Parser SRI + roundtrip Dapper end-to-end contra SQLite real):

```powershell
dotnet test
```

Resultado esperado:

```
Passed!  - Failed: 0, Passed: 10, Skipped: 0  (Domain.Tests)
Passed!  - Failed: 0, Passed:  9, Skipped: 0  (Infrastructure.Tests)
```

Para correr una suite específica:

```powershell
dotnet test tests/XmlEmailSender.Domain.Tests
dotnet test tests/XmlEmailSender.Infrastructure.Tests --filter "FullyQualifiedName~Parsing"
```

---

## Configuración

Todo se controla desde [src/XmlEmailSender.API/appsettings.json](src/XmlEmailSender.API/appsettings.json).

### Cambiar el provider de base de datos

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "Default": "Data Source=xmlemailsender.db"
  }
}
```

Para Postgres:

```json
{
  "Database": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=xmlemailsender;Username=xmlemailsender;Password=Dev_Passw0rd!"
  }
}
```

El schema runner detectará automáticamente que es Postgres y aplicará el script `001_initial.postgres.sql` en lugar del `.sqlite.sql`.

### CORS (frontend en otro dominio)

En desarrollo, los orígenes `http://localhost:5173` y `http://localhost:3000` están permitidos automáticamente. En producción declara tus dominios:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.tudominio.com"
    ]
  }
}
```

### Logs

Por defecto los logs van a la consola y a archivos rotados diariamente en `src/XmlEmailSender.API/logs/xmlemailsender-YYYYMMDD.log`. Cambia el nivel desde `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

---

## Hoja de ruta (fases)

| Fase | Estado | Descripción |
|---|---|---|
| **1 — Setup** | ✅ Completada | Solución, proyectos, referencias, NuGet, Program.cs, frontend scaffold. |
| **2 — Domain + Parser SRI + Dapper** | ✅ Completada | Entidades, ValueObjects, parser SRI con Strategy, persistencia Dapper con schema runner, tests end-to-end. |
| **3 — RIDE PDF + QR** | ✅ Completada | Plantillas QuestPDF para Factura/NotaCrédito/Retención, código QR con URL del SRI, Factory Pattern, IVA dinámico por bucket. |
| **4 — CQRS + envío de correo** | ✅ Completada | Commands/Queries con MediatR, pipeline behaviors (Logging + Validation + UoW), MailKit con cifrado de password SMTP via DataProtection. |
| **5 — Controllers REST + UI completa** | ✅ Completada | Endpoints REST (`/api/documents`, `/api/smtp-config`), upload múltiple con react-dropzone, tabla de historial con modal de detalle/reenvío, CRUD SMTP, dashboard con Recharts. ProblemDetails (RFC 7807) en errores. |
| **6 — Deploy Linux** | 🔜 | Ubuntu 22.04, Nginx + Let's Encrypt, systemd para correr la API como servicio. |

---

## Troubleshooting

### `dotnet build` falla con `error NU1101: Unable to find package …`

Verifica conectividad con NuGet:
```powershell
dotnet nuget locals all --clear
dotnet restore
```

Si estás detrás de un proxy corporativo, configura `~/.nuget/NuGet/NuGet.Config` con tus credenciales o usa un mirror interno.

### El frontend no puede conectar con la API

1. La API debe estar corriendo en el puerto **5099** (revisa la salida del terminal 1).
2. El proxy de Vite está en [frontend/vite.config.ts](frontend/vite.config.ts):
   ```ts
   server: { proxy: { '/api': 'http://localhost:5099' } }
   ```
   Si tu API arrancó en otro puerto, edita ese campo.

### `git push` falla con `self-signed certificate in certificate chain`

Red corporativa con proxy SSL. Solución por repo (sin reducir seguridad global):

```powershell
git config --local http.sslBackend schannel
git push
```

Esto le indica a Git que use el almacén de certificados de Windows (que sí tiene el cert raíz corporativo instalado por IT) en lugar del bundle propio de OpenSSL. Solo afecta a este repo.

**Nunca** uses `git config --global http.sslVerify false` — eso te deja expuesto a MITM en todos tus repos para siempre.

### Warning `NU1903 AutoMapper has a known high severity vulnerability`

CVE conocido en AutoMapper 13.0.1. Pendiente de actualizar en una iteración futura — no afecta funcionalidad ni la seguridad runtime del proyecto local. Lo mismo aplica al `NU1902` de MailKit 4.8.0.

### `dotnet test` falla con `Invalid cast from 'System.String' to 'System.Guid'`

Significa que el `DbConnectionFactory` no registró los Dapper type handlers para SQLite. Esto debería ocurrir automáticamente — si lo ves, verifica que `EnsureDapperHandlers` se ejecuta en el constructor de la factory ([DbConnectionFactory.cs:25](src/XmlEmailSender.Infrastructure/Persistence/DbConnectionFactory.cs#L25)).

---

## Licencia

Por definir (proyecto de portafolio).

## Contacto

[Xavier Quiroz](https://github.com/xavierquiroz1998)
