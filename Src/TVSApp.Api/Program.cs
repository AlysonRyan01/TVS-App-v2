using QuestPDF.Infrastructure;
using TVS_App.Api.Common;
using TVS_App.Api.SignalR;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;
builder.AddCorsConfiguration();
builder.AddSqlServer();
builder.AddIdentity();
builder.AddAuthentication();
builder.AddJwtService();
builder.ConfigureJsonSerializer();
builder.AddDependencies();
builder.Services.AddSignalR();

if (builder.Environment.IsDevelopment())
    builder.AddSwagger();


var app = builder.Build();

app.UseCors(builder.Configuration["Cors:PolicyName"]!);

app.AddMigrations();

app.MapHub<ServiceOrderHub>("/osHub");
app.AddAuthorization();
app.AddEndpoints();

if (app.Environment.IsDevelopment())
    app.AddSwagger();

app.MapGet("/", () => "API RODANDO");

app.Run();
