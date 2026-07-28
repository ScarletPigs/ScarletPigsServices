using Aspire.Hosting;
using Ridder.Hosting.Dokploy;
using ScarletPigsServices.ServiceReferences;

var builder = DistributedApplication.CreateBuilder(args);

// DEPLOYMENT ENVIRONMENT
var dokploy = builder.AddDokployEnvironment("scarletpigs")
    .WithHostedRegistry();


// ENVIRONMENT PARAMETERS
var DISCORD_TOKEN = builder.AddParameterFromConfiguration("DISCORDTOKEN", "DISCORDTOKEN", true).WithDescription("Discord bot token for authentication, used for the Discord bot's login and API access.");
var DISCORD_CLIENT_ID = builder.AddParameterFromConfiguration("DISCORDCLIENTID", "DISCORDCLIENTID", true).WithDescription("Discord client ID for OAuth2 authentication, used for the Discord bot's login and API access.");
var DISCORD_CLIENT_SECRET = builder.AddParameterFromConfiguration("DISCORDCLIENTSECRET", "DISCORDCLIENTSECRET", true).WithDescription("Discord client secret for OAuth2 authentication, used for the Discord bot's login and API access.");
var HAVOC_SERVER_FTP_HOST = builder.AddParameterFromConfiguration("havoc-server-ftp-host", "HAVOC_SERVER_FTP_HOST", false).WithDescription("Hostname for the Havoc FTP server used by the website mission upload tool.");
var HAVOC_SERVER_FTP_PORT = builder.AddParameterFromConfiguration("havoc-server-ftp-port", "HAVOC_SERVER_FTP_PORT", false).WithDescription("Port for the Havoc FTP server used by the website mission upload tool.");
var HAVOC_FTP_USER = builder.AddParameterFromConfiguration("havoc-ftp-user", "HAVOC_FTP_USER", true).WithDescription("Username for the Havoc FTP server used by the website mission upload tool.");
var HAVOC_FTP_PASSWORD = builder.AddParameterFromConfiguration("havoc-ftp-password", "HAVOC_FTP_PASSWORD", true).WithDescription("Password for the Havoc FTP server used by the website mission upload tool.");
var HAVOC_SERVER_FTP_ROOT = builder.AddParameterFromConfiguration("havoc-server-ftp-root", "HAVOC_SERVER_FTP_ROOT", false).WithDescription("Root path used when listing and uploading missions through the website FTP integration.");
var HAVOC_HEADLESS_FTP_HOST = builder.AddParameterFromConfiguration("havoc-headless-ftp-host", "HAVOC_HEADLESS_FTP_HOST", false).WithDescription("Optional hostname for the headless FTP server used by the website command-line generator parity checks.");
var HAVOC_HEADLESS_FTP_PORT = builder.AddParameterFromConfiguration("havoc-headless-ftp-port", "HAVOC_HEADLESS_FTP_PORT", false).WithDescription("Optional port for the headless FTP server used by the website command-line generator parity checks.");
var HAVOC_HEADLESS_FTP_USER = builder.AddParameterFromConfiguration("havoc-headless-ftp-user", "HAVOC_HEADLESS_FTP_USER", false).WithDescription("Optional username override for the headless FTP server used by the website command-line generator parity checks.");
var HAVOC_HEADLESS_FTP_PASSWORD = builder.AddParameterFromConfiguration("havoc-headless-ftp-password", "HAVOC_HEADLESS_FTP_PASSWORD", false).WithDescription("Optional password override for the headless FTP server used by the website command-line generator parity checks.");
var HAVOC_HEADLESS_FTP_ROOT = builder.AddParameterFromConfiguration("havoc-headless-ftp-root", "HAVOC_HEADLESS_FTP_ROOT", false).WithDescription("Optional root path used when checking headless FTP mod folders for website command-line parity.");
var CREATOR_ID = builder.AddParameterFromConfiguration("CREATORID", "CREATORID", true).WithDescription("The Discord user ID of the creator of the piglet bot, used for various operations.");
var GITHUB_TOKEN = builder.AddParameterFromConfiguration("GITHUBTOKEN", "GITHUBTOKEN", true).WithDescription("GitHub token for accessing repositories and performing actions on behalf of the bot.");
var SERVER_IP = builder.AddParameterFromConfiguration("server-ip", "SERVER_IP", false).WithDescription("Game server hostname or IP address queried by the Piglet bot for Discord presence.");
var SERVER_PORT = builder.AddParameterFromConfiguration("server-port", "SERVER_PORT", false).WithDescription("Game server port queried by the Piglet bot for Discord presence.");
var API_KEY = builder.AddParameterFromConfiguration("api-key", "API_KEY", true).WithDescription("At least 32 random bytes used to authenticate requests to the Scarlet Pigs API.");
var OCAP_SECRET = builder.AddParameterFromConfiguration("ocap-secret", "OCAP_SECRET", true).WithDescription("Shared secret used to authenticate OCAP recording uploads and sign admin sessions.");
var OCAP_ADMIN_STEAM_IDS = builder.AddParameterFromConfiguration("ocap-admin-steam-ids", "OCAP_AUTH_ADMINSTEAMIDS", false).WithDescription("Comma-separated Steam64 IDs authorized to access the OCAP administration pages.");
// Legacy Google credentials are used only by the manual API import endpoint.
var GOOGLE_SHEET_NAME = builder.AddParameterFromConfiguration("google-sheet-name", "GOOGLE_SHEET_NAME", true).WithDescription("Legacy workbook name used by the API's manual Google Sheets import.");
var TYPE = builder.AddParameterFromConfiguration("type", "TYPE", true).WithDescription("Google service account credential type used by the API import.");
var PROJECT_ID = builder.AddParameterFromConfiguration("project-id", "PROJECT_ID", true).WithDescription("Google Cloud project ID used by the API import.");
var PRIVATE_KEY_ID = builder.AddParameterFromConfiguration("private-key-id", "PRIVATE_KEY_ID", true).WithDescription("Google service account private key identifier used by the API import.");
var PRIVATE_KEY = builder.AddParameterFromConfiguration("private-key", "PRIVATE_KEY", true).WithDescription("Google service account private key used by the API import.");
var CLIENT_EMAIL = builder.AddParameterFromConfiguration("client-email", "CLIENT_EMAIL", true).WithDescription("Google service account client email used by the API import.");
var CLIENT_ID = builder.AddParameterFromConfiguration("client-id", "CLIENT_ID", true).WithDescription("Google service account client ID used by the API import.");
var AUTH_URI = builder.AddParameterFromConfiguration("auth-uri", "AUTH_URI", true).WithDescription("Google service account authorization URI used by the API import.");
var TOKEN_URI = builder.AddParameterFromConfiguration("token-uri", "TOKEN_URI", true).WithDescription("Google service account token URI used by the API import.");
var AUTH_PROVIDER_X509_CERT_URL = builder.AddParameterFromConfiguration("auth-provider-x509-cert-url", "AUTH_PROVIDER_X509_CERT_URL", true).WithDescription("Google auth provider certificate URL used by the API import.");
var CLIENT_X509_CERT_URL = builder.AddParameterFromConfiguration("client-x509-cert-url", "CLIENT_X509_CERT_URL", true).WithDescription("Google service account certificate URL used by the API import.");




// DATABASES

// Postgres Database
var dbService = builder.AddPostgres(ServiceRefs.DB_SERVER)
    .WithDataVolume("scarletpigs-postgres-data")
    .WithPgWeb()
    .PublishToDokploy(dokploy);
var scarletpigsDb = dbService.AddDatabase(ServiceRefs.DB);




// SERVICES

// Api Service
var apiService = builder.AddProject<Projects.ScarletPigsServices_Api>(ServiceRefs.API)
    .WithExternalHttpEndpoints()
    .WithEnvironment("ApiKey__Key", API_KEY)
    .WithEnvironment("HAVOC_SERVER_FTP_HOST", HAVOC_SERVER_FTP_HOST)
    .WithEnvironment("HAVOC_SERVER_FTP_PORT", HAVOC_SERVER_FTP_PORT)
    .WithEnvironment("HAVOC_FTP_USER", HAVOC_FTP_USER)
    .WithEnvironment("HAVOC_FTP_PASSWORD", HAVOC_FTP_PASSWORD)
    .WithEnvironment("HAVOC_SERVER_FTP_ROOT", HAVOC_SERVER_FTP_ROOT)
    .WithEnvironment("HAVOC_HEADLESS_FTP_HOST", HAVOC_HEADLESS_FTP_HOST)
    .WithEnvironment("HAVOC_HEADLESS_FTP_PORT", HAVOC_HEADLESS_FTP_PORT)
    .WithEnvironment("HAVOC_HEADLESS_FTP_USER", HAVOC_HEADLESS_FTP_USER)
    .WithEnvironment("HAVOC_HEADLESS_FTP_PASSWORD", HAVOC_HEADLESS_FTP_PASSWORD)
    .WithEnvironment("HAVOC_HEADLESS_FTP_ROOT", HAVOC_HEADLESS_FTP_ROOT)
    .WithEnvironment("GoogleSheetsImport__SpreadsheetName", GOOGLE_SHEET_NAME)
    .WithEnvironment("GoogleSheetsImport__Type", TYPE)
    .WithEnvironment("GoogleSheetsImport__ProjectId", PROJECT_ID)
    .WithEnvironment("GoogleSheetsImport__PrivateKeyId", PRIVATE_KEY_ID)
    .WithEnvironment("GoogleSheetsImport__PrivateKey", PRIVATE_KEY)
    .WithEnvironment("GoogleSheetsImport__ClientEmail", CLIENT_EMAIL)
    .WithEnvironment("GoogleSheetsImport__ClientId", CLIENT_ID)
    .WithEnvironment("GoogleSheetsImport__AuthUri", AUTH_URI)
    .WithEnvironment("GoogleSheetsImport__TokenUri", TOKEN_URI)
    .WithEnvironment("GoogleSheetsImport__AuthProviderX509CertUrl", AUTH_PROVIDER_X509_CERT_URL)
    .WithEnvironment("GoogleSheetsImport__ClientX509CertUrl", CLIENT_X509_CERT_URL)
    .WithReference(scarletpigsDb)
    .PublishToDokploy(dokploy, options => options
        .WithDomain("http", "api.scarletpigs.com")
        .WithDomain("https", "api.scarletpigs.com"));

// Entity Framework migrations
var migrations = apiService
    .AddEFMigrations(ServiceRefs.MIGRATIONS)
    .WithMigrationsProject("../../src/ScarletPigsServices.Data/ScarletPigsServices.Data.csproj")
    .WithReference(scarletpigsDb)
    .WaitFor(dbService)
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationBundle(
        targetRuntime: "linux-x64",
        publishContainer: true,
        baseImage: "mcr.microsoft.com/dotnet/aspnet:10.0")
    .PublishToDokploy(dokploy, options => options.RunOnce = true);

apiService.WaitFor(migrations);

// OCAP mission recording service
var ocapService = builder.AddContainer(ServiceRefs.OCAP, "ocap2/web", "2.1.1")
    .WithImageRegistry("ghcr.io")
    .WithHttpEndpoint(targetPort: 5000, name: "http")
    .WithHttpHealthCheck("/api/healthcheck")
    .WithEnvironment("OCAP_SECRET", OCAP_SECRET)
    .WithEnvironment("OCAP_AUTH_ADMINSTEAMIDS", OCAP_ADMIN_STEAM_IDS)
    .WithEnvironment("OCAP_CONVERSION_ENABLED", "true")
    .WithEnvironment("OCAP_STREAMING_ENABLED", "true")
    .WithEnvironment("OCAP_LOGGER", "true");

if (builder.ExecutionContext.IsRunMode)
{
    ocapService
        .WithBindMount("../../volumes/ocap/data", "/var/lib/ocap/data")
        .WithBindMount("../../volumes/ocap/maps", "/var/lib/ocap/maps")
        .WithBindMount("../../volumes/ocap/db", "/var/lib/ocap/db");
}
else
{
    // Dokploy terminates TLS before forwarding HTTPS traffic to OCAP's HTTP port.
    ocapService
        .WithHttpsEndpoint(targetPort: 5000, name: "https")
        .WithVolume("scarletpigs-ocap-data", "/var/lib/ocap/data")
        .WithVolume("scarletpigs-ocap-maps", "/var/lib/ocap/maps")
        .WithVolume("scarletpigs-ocap-db", "/var/lib/ocap/db");
}

ocapService
    .WithExternalHttpEndpoints()
    .PublishToDokploy(dokploy, options => options
        .WithDomain("http", "aar.scarletpigs.com")
        .WithDomain("https", "aar.scarletpigs.com"));

// Web Frontend Service
/*
builder.AddProject<Projects.ScarletPigsServices_Website>(ServiceRefs.WEBSITE)
    .WithEnvironment(DISCORD_CLIENT_ID.Resource.Name, DISCORD_CLIENT_ID)
    .WithEnvironment(DISCORD_CLIENT_SECRET.Resource.Name, DISCORD_CLIENT_SECRET)
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
*/

/*
builder.AddViteApp(ServiceRefs.WEBSITE_SPA, "../../src/ScarletPigsServices.Website.Spa")
    .WithEnvironment("VITE_AUTH_AUTHORITY", "https://keycloak.scarletpigs.com/realms/ScarletPigs")
    .WithEnvironment("VITE_AUTH_CLIENT_ID", "scarletpigsclient")
    .WithEnvironment("VITE_AUTH_SCOPE", "openid profile email roles")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
*/

// Discord bot service
// This could totally be switched over to .Net in the future
var piglet = builder.AddPythonApp(ServiceRefs.DISCORD_BOT, "../../src/ScarletPigsServices.Piglet", "main.py")
    .WithVirtualEnvironment(".venv")
    .WithPip()
    .WithEnvironment("DISCORD_TOKEN", DISCORD_TOKEN)
    .WithEnvironment("CREATOR_ID", CREATOR_ID)
    .WithEnvironment("GITHUB_TOKEN", GITHUB_TOKEN)
    .WithEnvironment("SERVER_IP", SERVER_IP)
    .WithEnvironment("SERVER_PORT", SERVER_PORT)
    .WithEnvironment("SCARLETPIGS_API", apiService.GetEndpoint("http"))
    .WithEnvironment("SCARLETPIGS_API_KEY", API_KEY)
    .WithReference(apiService)
    .PublishAsDockerFile()
    .PublishToDokploy(dokploy)
    .WithExplicitStart();


builder.Build().Run();
