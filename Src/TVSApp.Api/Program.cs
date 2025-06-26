using TVS_App.Api.Common;

var builder = WebApplication.CreateBuilder(args);
builder.AddQuestPdfConfiguration();
builder.AddCorsConfiguration();
builder.AddSqlServer();
builder.AddIdentity();
builder.AddAuthentication();
builder.AddJwtService();
builder.ConfigureJsonSerializer();
builder.AddDependencies();
builder.AddSignalR();
builder.AddSwagger();

var app = builder.Build();
app.AddExceptionMiddleware();
app.UseCors(builder.Configuration["Cors:PolicyName"]!);
app.AddSwagger();
app.AddMigrations();
app.AddSignalR();
app.AddAuthorization();
app.AddEndpoints();

app.MapGet("/", () => "API RODANDO");

app.Run();
