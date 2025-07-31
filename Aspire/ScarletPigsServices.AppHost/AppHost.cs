using Aspire.Hosting;
using ScarletPigsServices.ServiceReferences;

var builder = DistributedApplication.CreateBuilder(args);

// SETUP ENVIRONMENT
var compose = builder.AddDockerComposeEnvironment("prod");


// ENVIRONMENT PARAMETERS
var DISCORD_TOKEN = builder.AddParameterFromConfiguration("DISCORDTOKEN", "DISCORDTOKEN", true);
var DISCORD_CLIENT_ID = builder.AddParameterFromConfiguration("DISCORDCLIENTID", "DISCORDCLIENTID", true);
var DISCORD_CLIENT_SECRET = builder.AddParameterFromConfiguration("DISCORDCLIENTSECRET", "DISCORDCLIENTSECRET", true);
var AUTH_PROVIDER_X509_CERT_URL = builder.AddParameterFromConfiguration("AUTHPROVIDERX509CERTURL", "AUTHPROVIDERX509CERTURL", true);
var AUTH_URI = builder.AddParameterFromConfiguration("AUTHURI", "AUTHURI", true);
var CLIENT_EMAIL = builder.AddParameterFromConfiguration("CLIENTEMAIL", "CLIENTEMAIL", true);
var CLIENT_ID = builder.AddParameterFromConfiguration("CLIENTID", "CLIENTID", true);
var CLIENT_X509_CERT_URL = builder.AddParameterFromConfiguration("CLIENTX509CERTURL", "CLIENTX509CERTURL", true);
var CREATOR_ID = builder.AddParameterFromConfiguration("CREATORID", "CREATORID", true);
var GITHUB_PASSWORD = builder.AddParameterFromConfiguration("GITHUBPASSWORD", "GITHUBPASSWORD", true);
var GITHUB_USERNAME = builder.AddParameterFromConfiguration("GITHUBUSERNAME", "GITHUBUSERNAME", true);
var GITHUB_TOKEN = builder.AddParameterFromConfiguration("GITHUBTOKEN", "GITHUBTOKEN", true);
var GOOGLE_SHEET_NAME = builder.AddParameterFromConfiguration("GOOGLESHEETNAME", "GOOGLESHEETNAME", true);
var PRIVATE_KEY = builder.AddParameterFromConfiguration("PRIVATEKEY", "PRIVATEKEY", true);
var PRIVATE_KEY_ID = builder.AddParameterFromConfiguration("PRIVATEKEYID", "PRIVATEKEYID", true);
var PROJECT_ID = builder.AddParameterFromConfiguration("PROJECTID", "PROJECTID", true);
var SCARLETPIGS_API_URL = builder.AddParameterFromConfiguration("SCARLETPIGSAPIURL", "SCARLETPIGSAPIURL", true);
var SERVER_IP = builder.AddParameterFromConfiguration("SERVERIP", "SERVERIP", true);
var SERVER_PORT = builder.AddParameterFromConfiguration("SERVERPORT", "SERVERPORT", true);
var TOKEN_URI = builder.AddParameterFromConfiguration("TOKENURI", "TOKENURIsrc", true);
var TYPE = builder.AddParameterFromConfiguration("TYPE", "TYPE", true);




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
