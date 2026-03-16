using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OutboxPatterns.Application.Users;
using OutboxPatterns.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUserService, CreateUser>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
builder.Services.AddDbContext<OutboxDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("OutboxConnection"));
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Outbox Patterns API",
        Version = "v1",
        Description = "Outbox Pattern implementasyonunu gösteren örnek API.",
        Contact = new OpenApiContact
        {
            Name = "Geliştirici",
            Email = "developer@example.com"
        }
    });
});
builder.Services.AddHostedService<OutboxProcessor>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Outbox Patterns API v1");
        options.RoutePrefix = string.Empty;
    });
}
app.UseHttpsRedirection();  
app.UseAuthorization();
app.MapControllers();
OutboxPatterns.Application.Users.Endpoint.CreateNewUsers(app);

app.Run();
