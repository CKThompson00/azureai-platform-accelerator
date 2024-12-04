using Azure.Identity;
using Microsoft.Data.SqlClient;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;  
using Azure.AI.OpenAI.Chat;  
using OpenAI.Chat; 
using static System.Environment;  
using Azure.AI.Inference;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Use dependency injection to inject services into the application.
// builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
// builder.Services.AddSingleton<IVectorizationService, VectorizationService>();
// builder.Services.AddSingleton<MaintenanceCopilot, MaintenanceCopilot>();

// Create a single instance of the CosmosClient to be shared across the application.
// builder.Services.AddSingleton<CosmosClient>((_) =>
// {
//     CosmosClient client = new(
//         connectionString: builder.Configuration["CosmosDB:ConnectionString"]!
//     );
//     return client;
// });

// Create a single instance of the AzureOpenAIClient to be shared across the application.
// builder.Services.AddSingleton<AzureOpenAIClient>((_) =>
// {
//     var endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
//     var credentials = new AzureKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"]!);

//     var client = new AzureOpenAIClient(endpoint, credentials);
//     return client;
// });

var app = builder.Build();

string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");  
string deploymentName = GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_ID");  
string searchEndpoint = GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT");  
string searchIndex = GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX");  
string openAiApiKey = GetEnvironmentVariable("AZURE_OPENAI_KEY");  

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/**** Endpoints ****/
// This endpoint serves as the default landing page for the API.
app.MapGet("/", async () => 
{
    return "Welcome to the Contoso Suites Web API!";
})
    .WithName("Index")
    .WithOpenApi();

// This endpoint is used to send a message to the Azure OpenAI endpoint.
app.MapPost("/Chat", async Task<string> (HttpRequest request) =>
{
    var message = await Task.FromResult(request.Form["message"]);
    
    return "This endpoint is not yet available.";
})
    .WithName("Chat")
    .WithOpenApi();


app.Run();