using AiService.Endpoints;
using AiService.Providers;
using AiService.Repositories;
using AiService.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

//Enable Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var cfg = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//PostGresSQL + Vector

var dataSourceBuilder = new NpgsqlDataSourceBuilder(cfg.GetConnectionString("PgVector"));
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddSingleton<IPgVectorRepository, PgVectorRepository>();

//Embedding + Chat Provider
var provider = cfg["EmbeddingProvider"]?.ToLowerInvariant() ?? "Ollama";

//TODO: Once we have other providers
//switch(provider)
//{

//}

//Embeddings
builder.Services.AddHttpClient<IEmbeddingProvider, OllamaEmbeddingProvider>(client =>
{
    client.BaseAddress = new Uri(cfg["Ollama:BaseUrl"] ?? "http://localhost:11434/");
    client.Timeout = TimeSpan.FromMinutes(5);
});
//Chat 
builder.Services.AddHttpClient<IChatProvider, OllamaChatProvider>(client =>
{
    client.BaseAddress = new Uri(cfg["Ollama:BaseUrl"] ?? "http://localhost:11434/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

//Web Search Provider
builder.Services.AddSingleton<IWebSearchProvider, WebSearchProvider>();

//Chat Service
builder.Services.AddScoped<IChatService, ChatService>();

//Http Client Factory
builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(cfg["OcelotGateway:BaseUrl"] ?? "http://localhost:8010");
});

//Build app
var app = builder.Build();

//Enable swagger
if (app.Environment.IsDevelopment()) 
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

//EndPoints
app.MapEmbeddingTest();
app.MapChat();
app.MapSemanticData();
app.MapSemanticSearch();

app.MapGet("/healthz", () => Results.Ok("Ok"));

app.Run();