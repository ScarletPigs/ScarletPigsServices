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
    .WithEnvironment(CREATOR_ID.Resource.Name, CREATOR_ID)
    .WithEnvironment(GITHUB_TOKEN.Resource.Name, GITHUB_TOKEN)
    .WithEnvironment("SCARLETPIGS_API", apiService.GetEndpoint("http"))
    .WithReference(apiService)
    .PublishAsDockerFile()
    .WithExplicitStart();
#pragma warning restore ASPIREHOSTINGPYTHON001


builder.Build().Run();
