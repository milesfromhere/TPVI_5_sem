using ASPA0011_1.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var criticalLogger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var configPath = "appsettings.json";
if (!File.Exists(configPath))
{
    criticalLogger.Fatal("Critical error: Configuration file 'appsettings.json' not found. Application cannot start.");
    Environment.Exit(1);
}

try
{
    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
}
catch (Exception ex)
{
    criticalLogger.Fatal(ex, "Critical error: Failed to load configuration file 'appsettings.json'. Application cannot start.");
    Environment.Exit(1);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IChannelService, ChannelService>();

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            restrictedToMinimumLevel: LogEventLevel.Warning,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .WriteTo.File(
            new CompactJsonFormatter(),
            "logs/log-.json",
            rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: LogEventLevel.Verbose
        )
        .WriteTo.File(
            "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information
        )
        .CreateLogger();
}
catch (Exception ex)
{
    criticalLogger.Fatal(ex, "Critical error: Failed to create log files. Application cannot start.");
    Environment.Exit(1);
}

builder.Host.UseSerilog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    try
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                "logs/debug-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Verbose
            )
            .CreateLogger();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Critical error: Failed to create debug log file. Application cannot start.");
        Environment.Exit(1);
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application ASPA0011_1 started successfully");

app.Run();