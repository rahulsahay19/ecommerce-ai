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
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

//Embedding + Chat Provider
var provider = cfg["EmbeddingProvider"]?.ToLowerInvariant() ?? "Ollama";

switch (provider)
{
    case "openai":
        // Embeddings
        builder.Services.AddHttpClient<IEmbeddingProvider, OpenAIEmbeddingProvider>(client =>
        {
            client.BaseAddress = new Uri(cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg["OpenAI:ApiKey"]);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        // Chat
        builder.Services.AddHttpClient<IChatProvider, OpenAIChatProvider>(client =>
        {
            client.BaseAddress = new Uri(cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cfg["OpenAI:ApiKey"]);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        break;

    case "azureopenai":
        // Embeddings
        builder.Services.AddHttpClient<IEmbeddingProvider, AzureOpenAIEmbeddingProvider>(client =>
        {
            client.BaseAddress = new Uri(cfg["AzureOpenAI:EndPoint"]
                ?? throw new InvalidOperationException("Missing AzureOpenAI:EndPoint"));
            client.DefaultRequestHeaders.Add("api-key", cfg["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("Missing AzureOpenAI:ApiKey"));
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        // Chat
        builder.Services.AddHttpClient<IChatProvider, AzureOpenAIChatProvider>(client =>
        {
            client.BaseAddress = new Uri(cfg["AzureOpenAI:EndPoint"]
                ?? throw new InvalidOperationException("Missing AzureOpenAI:EndPoint"));
            client.DefaultRequestHeaders.Add("api-key", cfg["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("Missing AzureOpenAI:ApiKey"));
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        break;

    default: // ollama
        // Embeddings
        builder.Services.AddHttpClient<IEmbeddingProvider, OllamaEmbeddingProvider>(client =>
        {
            client.BaseAddress = new Uri(cfg["Ollama:BaseUrl"] ?? "http://ollama:11434/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        // Chat
        builder.Services.AddHttpClient<IChatProvider, OllamaChatProvider>(client =>
        {
            client.BaseAddress = new Uri(cfg["Ollama:BaseUrl"] ?? "http://ollama:11434/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        break;
}

//Web Search Provider
builder.Services.AddSingleton<IWebSearchProvider, WebSearchProvider>();

//Service
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IVoiceService, VoiceService>();


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
app.MapVoice();

app.MapGet("/healthz", () => Results.Ok("Ok"));

app.Run();