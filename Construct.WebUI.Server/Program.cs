using BeamClassLibrary.Services;
using Construct.Application;
using Construct.Application.Interfaces;
using Construct.Application.Services;
using Construct.Application.Serialization;
using Construct.Domain;
using Construct.Domain.Entities;
using Construct.Infrastructure;
using Construct.WebUI.Server.Authentication;
using Construct.WebUI.Server.Components;
using Construct.WebUI.Server.Interop;
using Construct.WebUI.Server.Shared.Data;
using Construct.WebUI.Server.Shared.SampleData;
using ExportFactory.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Graph;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Validators;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Globalization;
using System.Text.Json;

const string MS_OIDC_SCHEME = "MicrosoftOidc";

// ? Registreer MaterialenDictionaryConverter in BEIDE ProjectJsonOptions
// Default wordt gebruikt door UndoRedoService
// Fast wordt gebruikt door ProjectFileService
var converter = new MaterialenDictionaryConverter();
ProjectJsonOptions.Default.Converters.Insert(0, converter);
ProjectJsonOptions.Fast.Converters.Insert(0, converter);




var builder = WebApplication.CreateBuilder(args);
var culture = CultureInfo.InvariantCulture;
var cultureUI = CultureInfo.GetCultureInfoByIetfLanguageTag("nl-NL"); 

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = cultureUI;



builder.Services.AddHttpClient();

// instelling of we authenticatie gebruiken
var gebruikAuth = builder.Configuration.GetValue<bool>("GebruikAuthenticatie:Enabled", true);

if (gebruikAuth)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = MS_OIDC_SCHEME;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(MS_OIDC_SCHEME, options =>
    {
        var oidcSection = builder.Configuration.GetSection("Authentication:Schemes:MicrosoftOidc");
        oidcSection.Bind(options);

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";

        var microsoftIssuerValidator = AadIssuerValidator.GetAadIssuerValidator(options.Authority);
        options.TokenValidationParameters.IssuerValidator = microsoftIssuerValidator.Validate;

    });


    builder.Services.AddInMemoryTokenCaches();
    builder.Services.ConfigureCookieOidc(CookieAuthenticationDefaults.AuthenticationScheme, MS_OIDC_SCHEME);
    builder.Services.AddAuthorization();
}
else
{
    // Dummy auth
    builder.Services.AddAuthentication("NoAuth")
        .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("NoAuth", options =>
        {
            options.TimeProvider = TimeProvider.System;
        });
    builder.Services.AddAuthorization();
}


builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    //.AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

builder.Services.AddHttpContextAccessor(); // Voor HttpContext in de GraphServiceClient
builder.Services.AddScoped<IAuthenticationProvider, HttpContextAuthenticationProvider>(); // eigen implementatie van IAuthenticationProvider
builder.Services.AddScoped(sp =>
{
    var authProvider = sp.GetRequiredService<IAuthenticationProvider>();
    return new GraphServiceClient(authProvider);
}); // GraphServiceClient met de eigen IAuthenticationProvider



builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Add services (FluentUI)
builder.Services.AddFluentUIComponents();

// Data
builder.Services.AddScoped<DataSource>(); // mock-up voor demonstratie
builder.Services.AddScoped<EurocodeData>(); // vaste data voor eurocode 

//register the service with the dependency injection (DI) container so it can be injected into Blazor components or ViewModels.
builder.Services.AddScoped<IProjectStateService, ProjectStateService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileDownloadService, FileDownloadService>();
builder.Services.AddScoped<IProjectFileService, ProjectFileService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAssemblageService, AssemblageService>();
builder.Services.AddScoped<IAppSettingService, AppSettingService>();
builder.Services.AddScoped<IGraphDriveService, GraphDriveService>();
builder.Services.AddScoped<ILogoService, LogoService>(); // LogoService voor het ophalen van logo's hiervoor zo scoped uitermate geschikt zijn.
builder.Services.AddScoped<ISvgToBitmapService, SvgToBitmapJsService>();
builder.Services.AddScoped<ExportService>();


//builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<ChartService>();
builder.Services.AddScoped<GeometryService>();
builder.Services.AddScoped<MigraDocCreator>();


builder.Services.AddSingleton<ExportFactory.Services.DataTableMappingService>();
builder.Services.AddSingleton<EurocodeRazorClassLibrary.Services.ReadOnlyService>();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddScoped(typeof(IUndoRedoService<>), typeof(UndoRedoService<>));


builder.Services.AddScoped<IUndoRedoService<ProjectEntity>>(_ =>
    new UndoRedoService<ProjectEntity>(entity =>
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(entity, ProjectJsonOptions.Default);
        var copy = JsonSerializer.Deserialize<ProjectEntity>(jsonBytes, ProjectJsonOptions.Default)!;
        copy.InitAll(); // of lichtere InitDataOnly() als je die hebt
        return copy;
    }));








// ----------------------
// Opbouw van de pipeline
// ----------------------
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);// Global error handling

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging(); // Enable WebAssembly debugging in development mode
}



// Zonder deze regel hieronder werkt de 404 pagina niet. Maar de pagina zelf wordt overschreven door de <NotFound> in de Routes.razor... 
app.UseStatusCodePagesWithReExecute("/PageNotFound"); // Redirect to the custom 404 page 

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();
app.UseAntiforgery();

if (gebruikAuth)
{
    app.UseAuthentication(); // Eerst authenticatie (wie)
    app.UseAuthorization(); // Dan autorisatie (wat)
}


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();



