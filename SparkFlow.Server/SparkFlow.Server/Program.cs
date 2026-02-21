using SparkFlow.Server.Endpoints;
using SparkFlow.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FlowStore>();
builder.Services.AddSingleton<LogStore>();
builder.Services.AddSingleton<UpdateStore>();

var app = builder.Build();

// ✅ API Key Middleware (protect everything except "/")
app.Use(async (ctx, next) =>
{
    var apiKey = app.Configuration["API_KEY"]; // from env vars

    // لو مفيش API_KEY متسجل => شغال بدون حماية (مفيد محليًا)
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        // اسمح للـ health check بدون مفتاح
        if (ctx.Request.Path == "/")
        {
            await next();
            return;
        }

        // تحقق من الهيدر
        if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var provided) ||
            !string.Equals(provided.ToString(), apiKey, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "unauthorized" });
            return;
        }
    }

    await next();
});

app.MapGet("/", () => new
{
    ok = true,
    name = "SparkFlow.Server",
    utc = DateTime.UtcNow
});

app.MapFlowEndpoints();
app.MapLogEndpoints();
app.MapBootstrapEndpoints();

app.Run();