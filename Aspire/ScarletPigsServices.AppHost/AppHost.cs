using Aspire.Hosting;
using ScarletPigsServices.ServiceReferences;

var builder = DistributedApplication.CreateBuilder(args);

// SETUP ENVIRONMENT
var compose = builder.AddDockerComposeEnvironment("prod");


// ENVIRONMENT PARAMETERS
var DISCORD_TOKEN = builder.AddParameterFromConfiguration("DISCORD_TOKEN", "DISCORD_TOKEN", true);
var DISCORD_CLIENT_ID = builder.AddParameterFromConfiguration("DISCORD_CLIENT_ID", "DISCORD_CLIENT_ID", true);
var DISCORD_CLIENT_SECRET = builder.AddParameterFromConfiguration("DISCORD_CLIENT_SECRET", "DISCORD_CLIENT_SECRET", true);
var AUTH_PROVIDER_X509_CERT_URL = builder.AddParameterFromConfiguration("AUTH_PROVIDER_X509_CERT_URL", "AUTH_PROVIDER_X509_CERT_URL", true);
var AUTH_URI = builder.AddParameterFromConfiguration("AUTH_URI", "AUTH_URI", true);
var CLIENT_EMAIL = builder.AddParameterFromConfiguration("CLIENT_EMAIL", "CLIENT_EMAIL", true);
var CLIENT_ID = builder.AddParameterFromConfiguration("CLIENT_ID", "CLIENT_ID", true);
var CLIENT_X509_CERT_URL = builder.AddParameterFromConfiguration("CLIENT_X509_CERT_URL", "CLIENT_X509_CERT_URL", true);
var CREATOR_ID = builder.AddParameterFromConfiguration("CREATOR_ID", "CREATOR_ID", true);
var GITHUB_PASSWORD = builder.AddParameterFromConfiguration("GITHUB_PASSWORD", "GITHUB_PASSWORD", true);
var GITHUB_USERNAME = builder.AddParameterFromConfiguration("GITHUB_USERNAME", "GITHUB_USERNAME", true);
var GITHUB_TOKEN = builder.AddParameterFromConfiguration("GITHUB_TOKEN", "GITHUB_TOKEN", true);
var GOOGLE_SHEET_NAME = builder.AddParameterFromConfiguration("GOOGLE_SHEET_NAME", "GOOGLE_SHEET_NAME", true);
var PRIVATE_KEY = builder.AddParameterFromConfiguration("PRIVATE_KEY", "PRIVATE_KEY", true);
var PRIVATE_KEY_ID = builder.AddParameterFromConfiguration("PRIVATE_KEY_ID", "PRIVATE_KEY_ID", true);
var PROJECT_ID = builder.AddParameterFromConfiguration("PROJECT_ID", "PROJECT_ID", true);
var SCARLETPIGS_API_URL = builder.AddParameterFromConfiguration("SCARLETPIGS_API_URL", "SCARLETPIGS_API_URL", true);
var SERVER_IP = builder.AddParameterFromConfiguration("SERVER_IP", "SERVER_IP", true);
var SERVER_PORT = builder.AddParameterFromConfiguration("SERVER_PORT", "SERVER_PORT", true);
var TOKEN_URI = builder.AddParameterFromConfiguration("TOKEN_URI", "TOKEN_URI", true);
var TYPE = builder.AddParameterFromConfiguration("TYPE", "TYPE", true);




// DATABASES

// Postgres Database
var dbService = builder.AddPostgres(ServiceRefs.DB_SERVER)
    .WithPgWeb();
var scarletpigsDb = dbService.AddDatabase(ServiceRefs.DB);




// SERVICES

// Migration Service
var migrationservice = builder.AddProject<Projects.ScarletPigsServices_MigrationService>(ServiceRefs.MIGRATION_SERVICE);

// Api Service
var apiService = builder.AddProject<Projects.ScarletPigsServices_Api>(ServiceRefs.API)
    .WaitForCompletion(migrationservice)
    .WithReference(scarletpigsDb)
    .WithHttpHealthCheck("/health");

// Web Frontend Service
builder.AddProject<Projects.ScarletPigsServices_Website>(ServiceRefs.WEBSITE)
    .WithEnvironment(DISCORD_CLIENT_ID.Resource.Name, DISCORD_CLIENT_ID)
    .WithEnvironment(DISCORD_CLIENT_SECRET.Resource.Name, DISCORD_CLIENT_SECRET)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

// Discord bot service
// This could totally be switched over to .Net in the future
#pragma warning disable ASPIREHOSTINGPYTHON001
var piglet = builder.AddPythonApp(ServiceRefs.DISCORD_BOT, "../ScarletPigsServices.Piglet", "main.py")
    .WithEnvironment(DISCORD_TOKEN.Resource.Name, DISCORD_TOKEN)
    .WithEnvironment(AUTH_PROVIDER_X509_CERT_URL.Resource.Name, AUTH_PROVIDER_X509_CERT_URL)
    .WithEnvironment(AUTH_URI.Resource.Name, AUTH_URI)
    .WithEnvironment(CLIENT_EMAIL.Resource.Name, CLIENT_EMAIL)
    .WithEnvironment(CLIENT_ID.Resource.Name, CLIENT_ID)
    .WithEnvironment(CLIENT_X509_CERT_URL.Resource.Name, CLIENT_X509_CERT_URL)
    .WithEnvironment(CREATOR_ID.Resource.Name, CREATOR_ID)
    .WithEnvironment(GITHUB_PASSWORD.Resource.Name, GITHUB_PASSWORD)
    .WithEnvironment(GITHUB_USERNAME.Resource.Name, GITHUB_USERNAME)
    .WithEnvironment(GITHUB_TOKEN.Resource.Name, GITHUB_TOKEN)
    .WithEnvironment(GOOGLE_SHEET_NAME.Resource.Name, GOOGLE_SHEET_NAME)
    .WithEnvironment(PRIVATE_KEY.Resource.Name, PRIVATE_KEY)
    .WithEnvironment(PRIVATE_KEY_ID.Resource.Name, PRIVATE_KEY_ID)
    .WithEnvironment(PROJECT_ID.Resource.Name, PROJECT_ID)
    .WithEnvironment(SCARLETPIGS_API_URL.Resource.Name, SCARLETPIGS_API_URL)
    .WithEnvironment(SERVER_IP.Resource.Name, SERVER_IP)
    .WithEnvironment(SERVER_PORT.Resource.Name, SERVER_PORT)
    .WithEnvironment(TOKEN_URI.Resource.Name, TOKEN_URI)
    .WithEnvironment(TYPE.Resource.Name, TYPE)
    .WithReference(apiService)
    .PublishAsDockerFile()
    .WithExplicitStart();
#pragma warning restore ASPIREHOSTINGPYTHON001


builder.Build().Run();
