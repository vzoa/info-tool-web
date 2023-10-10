using Coravel;
using ZoaReference;
using ZoaReference.Components;
using ZoaReference.FeatureUtilities;

var builder = WebApplication.CreateBuilder(args);

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

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();