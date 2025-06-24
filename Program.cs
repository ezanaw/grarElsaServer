using Elsa.Alterations.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using EFCoreSecondLevelCacheInterceptor;
using EasyCaching.Core.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Assuming the implementation is called TaskDispatcher
builder.Services.AddScoped<ElsaServer.Custom.ITaskDispatcher, ElsaServer.Custom.TaskDispatcher>();


// **Elsa configuration**
builder.Services.AddElsa(elsa =>
{
    // Workflow Management
    elsa.UseWorkflowManagement(management =>
    {
        management.UseEntityFrameworkCore(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Elsa")));
    });

    // Workflow Runtime
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Elsa")));
    });

    // Identity & API
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options => options.SigningKey = "turXh889xVusqCRBz3SIzRapD90e6hgAw/1QYvdpKUo=";
        identity.UseAdminUserProvider();
    });
    // Configure ASP.NET authentication/authorization.
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

    // Expose Elsa API endpoints.
    elsa.UseWorkflowsApi();

    // Setup a SignalR hub for real-time updates from the server.
    elsa.UseRealTimeWorkflows();

    // Enable JavaScript workflows
    elsa.UseJavaScript(options => options.AllowClrAccess = true);

    // Enable C# workflow expressions.
    elsa.UseCSharp();

    // Enable HTTP activities.
    elsa.UseHttp();

    // Use timer activities.
    elsa.UseScheduling();


    // Enable Elsa Alterations
    elsa.UseAlterations();

    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
});

// **Add SignalR services**
builder.Services.AddSignalR();
// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-elsa-workflow-instance-id"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.

// Add Health Checks.
builder.Services.AddHealthChecks();

// Configure Redis Cache using configuration settings
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ElsaCache";  // Optional instance name
});

// Configure EF Core with Redis Cache for RuntimeElsaDbContext
builder.Services.AddDbContext<RuntimeElsaDbContext>(options =>
{
    // Use SQL Server for the database
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Elsa"),
        sqlOptions => sqlOptions.MigrationsAssembly("ElsaServer")  // Specify ElsaServer as the migrations assembly
    );

    // Add second-level caching using Redis and EasyCaching
    options.AddInterceptors(builder.Services.BuildServiceProvider().GetRequiredService<SecondLevelCacheInterceptor>());
});

// Configure EF Core Second-Level Cache
builder.Services.AddEFSecondLevelCache(options =>
{
options.UseEasyCachingCoreProvider("redis", isHybridCache: false)
       .ConfigureLogging(true)
       .UseCacheKeyPrefix("EF_");
});

builder.Services.AddEasyCaching(options =>
{
    options.UseRedis(config =>
    {
        // Retrieve the Redis connection string from appsettings.json
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        var redisDatabase = int.Parse(builder.Configuration["Redis:Database"]);  // Get the database number from appsettings.json

        // Parse the Redis connection string and configure the Redis server endpoint
        var redisEndpoint = redisConnectionString.Split(':')[0];  // Get the Redis host
        var redisPort = int.Parse(redisConnectionString.Split(':')[1]);  // Get the Redis port

        config.DBConfig.Endpoints.Add(new ServerEndPoint(redisEndpoint, redisPort));  // Set the Redis host and port
        config.DBConfig.Database = redisDatabase;  // Use the specified Redis database

        // Set the serializer to JSON
        config.SerializerName = "json";  // Ensure the serializer is configured as 'json'
    }, "redis");
});


// Build the web application.
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Configure web application's middleware pipeline.
app.UseCors();
app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();


app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 
// **Map controllers**
app.MapControllers();


app.Run();
