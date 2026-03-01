using AWS.Logger;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TodoApi.Application;
using TodoApi.Infrastructure.Configuration;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Repositories;
using TodoApi.Infrastructure.Security;
using TodoApi.Infrastructure.Sessions;
using TodoApi.Infrastructure.Storage;
using TodoApi.Middleware;
using TodoApi.Services;

var builder = WebApplication.CreateBuilder(args);

var bootstrapSettings = await AwsBootstrapConfiguration.LoadAsync(builder.Configuration);
builder.Configuration.AddInMemoryCollection(bootstrapSettings);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var awsEnabled = builder.Configuration.GetValue<bool?>("AWS:Enabled") ?? false;
if (awsEnabled)
{
    var region = builder.Configuration["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-northeast-1";
    var logGroup = builder.Configuration["AWS:LogGroup"] ?? "/ecs/todo-api";
    builder.Logging.AddAWSProvider(new AWSLoggerConfig
    {
        Region = region,
        LogGroup = logGroup
    });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<ISessionService, RedisSessionService>();
builder.Services.AddScoped<IAttachmentStorage, S3AttachmentStorage>();
builder.Services.AddScoped<DbMigrationRunner>();
builder.Services.AddScoped<DbSeeder>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (allowedOrigins.Length == 1 && allowedOrigins[0] == "*")
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("default");
app.UseHttpsRedirection();
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var migrationRunner = scope.ServiceProvider.GetRequiredService<DbMigrationRunner>();
    await migrationRunner.RunAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();
