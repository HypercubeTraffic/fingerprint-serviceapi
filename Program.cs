using FingerprintWebAPI.Services;
using FingerprintWebAPI.Hubs;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/fingerprint-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add SignalR for WebSocket communication
    builder.Services.AddSignalR();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
        
        options.AddPolicy("SignalRCors", policy =>
        {
            policy.AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetIsOriginAllowed(origin => true);
        });
    });

    // Register custom services
    builder.Services.AddSingleton<IFingerprintService, FingerprintService>();
    builder.Services.AddSingleton<IPreviewService, PreviewService>();
    builder.Services.AddSingleton<IWebSocketService, WebSocketService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseCors("AllowAll");

    app.UseAuthorization();

    app.MapControllers();

    // Map SignalR hub with CORS
    app.MapHub<FingerprintHub>("/ws/fingerprint").RequireCors("SignalRCors");

    // Serve the web interface at root
    app.MapFallbackToFile("index.html");

    Log.Information("Fingerprint Web API starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
