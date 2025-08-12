using Serilog;
using Serilog.Context;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ==== 1. Configure SQL Sink Column Options ====
var columnOptions = new ColumnOptions
{
    AdditionalColumns = new Collection<SqlColumn>
    {
        new SqlColumn("CorrelationId", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("RequestId", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("UserId", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("ServiceName", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("Environment", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("RequestPath", System.Data.SqlDbType.NVarChar) { DataLength = 500 },
        new SqlColumn("ClientIP", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("UserAgent", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("OperationName", System.Data.SqlDbType.NVarChar) { DataLength = 256 },
        new SqlColumn("ExecutionTimeMs", System.Data.SqlDbType.Int)
    }
};

// ==== 2. Configure Serilog ====
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithProperty("ServiceName", "OrderService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .WriteTo.MSSqlServer(
        connectionString: "Your connection string",
        tableName: "Logs",
        autoCreateSqlTable: true, // change to true if table should be created
        columnOptions: columnOptions
    )
    .CreateLogger();

// Hook Serilog into the host
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ==== 3. Middleware to add contextual logging fields ====
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    using (LogContext.PushProperty("RequestId", $"REQ-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}"))
    using (LogContext.PushProperty("UserId", context.User.Identity?.Name ?? "Anonymous"))
    using (LogContext.PushProperty("RequestPath", context.Request.Path))
    using (LogContext.PushProperty("ClientIP", context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"))
    using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
    using (LogContext.PushProperty("OperationName", "UnknownOperation"))
    {
        await next();

        stopwatch.Stop();
        LogContext.PushProperty("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
        Log.Information("Request completed in {ExecutionTimeMs} ms", stopwatch.ElapsedMilliseconds);
    }
});

// ==== 4. Swagger & HTTPS ====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// ==== 5. Test Endpoint ====
app.MapGet("/checkout", (HttpContext context) =>
{
    using (LogContext.PushProperty("OperationName", "CheckoutOrder"))
    {
        Log.Information("Processing checkout order");
    }
    return Results.Ok(new { Status = "Order Placed" });
});

app.MapControllers();

// ==== 6. Ensure logs flush on shutdown ====
AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

app.Run();

