using System.Diagnostics;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();


// App Health Status
app.MapGet("/health", () => Results.Ok(new {
    status = "Healthy"
})).WithName("Health");

app.MapGet("/version", () => Results.Ok(new {
    version = GetVersion()
})).WithName("Version");

app.Run();


// Get app current version 
static string GetVersion() {
    var asm = Assembly.GetExecutingAssembly();
    var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    if (!string.IsNullOrWhiteSpace(info)) return info;

    var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
    if (!string.IsNullOrWhiteSpace(file)) return file;

    var name = asm.GetName()?.Version?.ToString();
    return string.IsNullOrWhiteSpace(name) ? "unknown" : name!;
}