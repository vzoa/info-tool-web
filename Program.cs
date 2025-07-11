using Coravel;
using ZoaReference;
using ZoaReference.Components;
using ZoaReference.FeatureUtilities;

var builder = WebApplication.CreateBuilder(args);
if ((Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "") != "") {
    builder.WebHost.UseSentry();
}

builder.Host.UseSystemd();

// Required for Healthcheck
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add the global App Settings class to DI container
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionKeyName));

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Scan for and add services defined in feature modules
builder.Services.AddFeatureServices();

// Add Coravel scheduler
builder.Services.AddScheduler();

var app = builder.Build();

app.UseAntiforgery();

// Scan for and add schedulers
app.Services.UseSchedulers();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var cacheMaxAgeOneHour = (60 * 60).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneHour}");
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();