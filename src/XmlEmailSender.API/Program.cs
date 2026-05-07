using Serilog;
using XmlEmailSender.API.Middleware;
using XmlEmailSender.Application;
using XmlEmailSender.Infrastructure;
using XmlEmailSender.Infrastructure.Persistence.Schema;

var builder = WebApplication.CreateBuilder(args);

// Serilog desde appsettings + sinks por defecto.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/xmlemailsender-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Capas de la aplicación.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Permite hasta 50 MB combinados de archivos en multipart/form-data
// (útil al subir varios XMLs grandes a la vez).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 50L * 1024 * 1024;
});

// CORS — abierto en dev, restringido en prod.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            policy.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Aplica migraciones de schema antes de aceptar tráfico.
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<SchemaMigrationRunner>();
    await runner.RunAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Captura excepciones no controladas y devuelve ProblemDetails (RFC 7807).
// Va antes de cualquier middleware que pueda lanzar.
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Iniciando XmlEmailSender API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El host terminó de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}
