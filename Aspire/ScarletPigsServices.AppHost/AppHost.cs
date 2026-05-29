using Aspire.Hosting;
using ScarletPigsServices.ServiceReferences;

var builder = DistributedApplication.CreateBuilder(args);

// SETUP ENVIRONMENT
var compose = builder.AddDockerComposeEnvironment("prod");


// ENVIRONMENT PARAMETERS
var DISCORD_TOKEN = builder.AddParameterFromConfiguration("DISCORDTOKEN", "DISCORDTOKEN", true).WithDescription("Discord bot token for authentication, used for the Discord bot's login and API access.");
var DISCORD_CLIENT_ID = builder.AddParameterFromConfiguration("DISCORDCLIENTID", "DISCORDCLIENTID", true).WithDescription("Discord client ID for OAuth2 authentication, used for the Discord bot's login and API access.");
var DISCORD_CLIENT_SECRET = builder.AddParameterFromConfiguration("DISCORDCLIENTSECRET", "DISCORDCLIENTSECRET", true).WithDescription("Discord client secret for OAuth2 authentication, used for the Discord bot's login and API access.");
var CREATOR_ID = builder.AddParameterFromConfiguration("CREATORID", "CREATORID", true).WithDescription("The Discord user ID of the creator of the piglet bot, used for various operations.");
var GITHUB_TOKEN = builder.AddParameterFromConfiguration("GITHUBTOKEN", "GITHUBTOKEN", true).WithDescription("GitHub token for accessing repositories and performing actions on behalf of the bot.");
var GOOGLE_SHEET_NAME = builder.AddParameterFromConfiguration("GOOGLE_SHEET_NAME", "GOOGLE_SHEET_NAME", true).WithDescription("Google Sheets workbook name used by the Piglet bot for schedule and questionnaire data.");
var TYPE = builder.AddParameterFromConfiguration("TYPE", "TYPE", true).WithDescription("Google service account credential type for the Piglet bot.");
var PROJECT_ID = builder.AddParameterFromConfiguration("PROJECT_ID", "PROJECT_ID", true).WithDescription("Google Cloud project ID for the Piglet bot service account.");
var PRIVATE_KEY_ID = builder.AddParameterFromConfiguration("PRIVATE_KEY_ID", "PRIVATE_KEY_ID", true).WithDescription("Google service account private key identifier for the Piglet bot.");
var PRIVATE_KEY = builder.AddParameterFromConfiguration("PRIVATE_KEY", "PRIVATE_KEY", true).WithDescription("Google service account private key for the Piglet bot.");
var CLIENT_EMAIL = builder.AddParameterFromConfiguration("CLIENT_EMAIL", "CLIENT_EMAIL", true).WithDescription("Google service account client email for the Piglet bot.");
var CLIENT_ID = builder.AddParameterFromConfiguration("CLIENT_ID", "CLIENT_ID", true).WithDescription("Google service account client ID for the Piglet bot.");
var AUTH_URI = builder.AddParameterFromConfiguration("AUTH_URI", "AUTH_URI", true).WithDescription("Google service account authorization URI for the Piglet bot.");
var TOKEN_URI = builder.AddParameterFromConfiguration("TOKEN_URI", "TOKEN_URI", true).WithDescription("Google service account token URI for the Piglet bot.");
var AUTH_PROVIDER_X509_CERT_URL = builder.AddParameterFromConfiguration("AUTH_PROVIDER_X509_CERT_URL", "AUTH_PROVIDER_X509_CERT_URL", true).WithDescription("Google auth provider certificate URL for the Piglet bot.");
var CLIENT_X509_CERT_URL = builder.AddParameterFromConfiguration("CLIENT_X509_CERT_URL", "CLIENT_X509_CERT_URL", true).WithDescription("Google service account certificate URL for the Piglet bot.");




// DATABASES

// Postgres Database
var dbService = builder.AddPostgres(ServiceRefs.DB_SERVER)
    .WithPgWeb();
var scarletpigsDb = dbService.AddDatabase(ServiceRefs.DB);




// SERVICES

// Migration Service
var migrationservice = builder.AddProject<Projects.ScarletPigsServices_MigrationService>(ServiceRefs.MIGRATION_SERVICE)
    .WaitFor(dbService)
    .WithReference(scarletpigsDb);

// Api Service
var apiService = builder.AddProject<Projects.ScarletPigsServices_Api>(ServiceRefs.API)
    .WaitForCompletion(migrationservice)
    .WithReference(scarletpigsDb);

// Web Frontend Service
builder.AddProject<Projects.ScarletPigsServices_Website>(ServiceRefs.WEBSITE)
    .WithEnvironment(DISCORD_CLIENT_ID.Resource.Name, DISCORD_CLIENT_ID)
    .WithEnvironment(DISCORD_CLIENT_SECRET.Resource.Name, DISCORD_CLIENT_SECRET)
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

// Discord bot service
// This could totally be switched over to .Net in the future
#pragma warning disable ASPIREHOSTINGPYTHON001
var piglet = builder.AddPythonApp(ServiceRefs.DISCORD_BOT, "../../src/ScarletPigsServices.Piglet", "main.py")
    .WithEnvironment("DISCORD_TOKEN", DISCORD_TOKEN)
    .WithEnvironment("CREATOR_ID", CREATOR_ID)
    .WithEnvironment("GITHUB_TOKEN", GITHUB_TOKEN)
    .WithEnvironment("GOOGLE_SHEET_NAME", GOOGLE_SHEET_NAME)
    .WithEnvironment("TYPE", TYPE)
    .WithEnvironment("PROJECT_ID", PROJECT_ID)
    .WithEnvironment("PRIVATE_KEY_ID", PRIVATE_KEY_ID)
    .WithEnvironment("PRIVATE_KEY", PRIVATE_KEY)
    .WithEnvironment("CLIENT_EMAIL", CLIENT_EMAIL)
    .WithEnvironment("CLIENT_ID", CLIENT_ID)
    .WithEnvironment("AUTH_URI", AUTH_URI)
    .WithEnvironment("TOKEN_URI", TOKEN_URI)
    .WithEnvironment("AUTH_PROVIDER_X509_CERT_URL", AUTH_PROVIDER_X509_CERT_URL)
    .WithEnvironment("CLIENT_X509_CERT_URL", CLIENT_X509_CERT_URL)
    .WithEnvironment("SCARLETPIGS_API", apiService.GetEndpoint("http"))
    .WithReference(apiService)
    .PublishAsDockerFile()
    .WithExplicitStart();
#pragma warning restore ASPIREHOSTINGPYTHON001


builder.Build().Run();
