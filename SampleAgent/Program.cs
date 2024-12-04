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
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");  
string deploymentName = GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_ID");  
string searchEndpoint = GetEnvironmentVariable("AZURE_AI_SEARCH_ENDPOINT");  
// string searchIndex = GetEnvironmentVariable("AZURE_AI_SEARCH_INDEX");  
string searchIndex = "irs";
string openAiApiKey = GetEnvironmentVariable("AZURE_OPENAI_KEY");  


// Create a single instance of the AzureOpenAIClient to be shared across the application.
builder.Services.AddSingleton<AzureOpenAIClient>((_) =>
{
    var endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!);
    var credentials = new AzureKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"]!);

    var client = new AzureOpenAIClient(endpoint, credentials);
    return client;
});

var app = builder.Build();

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

app.MapGet("/SearchLoans", async (string question) => 
{
 #pragma warning disable AOAI001

    AzureKeyCredential credential = new(openAiApiKey); // Add your OpenAI API key here  
    AzureOpenAIClient azureClient = new(  
        new Uri(endpoint),  
        credential  
    );      
    ChatClient chatClient = azureClient.GetChatClient(deploymentName);

    ChatCompletionOptions options = new();
    DataSourceFieldMappings fieldMappings = new DataSourceFieldMappings();

    options.AddDataSource(new AzureSearchChatDataSource()
    {
        Endpoint = new Uri(searchEndpoint),
        IndexName = searchIndex,
        Strictness = 5,
        QueryType = "vector_semantic_hybrid",
        TopNDocuments = 3,
        VectorizationSource = DataSourceVectorizer.FromDeploymentName("text-embedding-ada-002"),
        Authentication = DataSourceAuthentication.FromApiKey(
            Environment.GetEnvironmentVariable("OYD_SEARCH_KEY")),
        FieldMappings = new DataSourceFieldMappings() 
        {

            FilePathFieldName  = "parent_id"
        }
    });
    options.Temperature = 0;

    ChatCompletion completion = chatClient.CompleteChat(
        [
            new SystemChatMessage("You are a helpful assistant that understand information about the IRS and taxes.  Only reference and answer questions from the reference document.  Reply with I don't know if you do not have an answer."),
            new UserChatMessage(question)
        ],
        options);

    ChatMessageContext onYourDataContext = completion.GetMessageContext();

    string answer = completion.Content[0].Text;

    if (onYourDataContext?.Intent is not null)
    {
        Console.WriteLine($"Intent: {onYourDataContext.Intent}");
    }
    int count = 1;

    string decodedStr;
    foreach (ChatCitation citation in onYourDataContext?.Citations ?? [])
    {
        Console.WriteLine($"Citation: {citation.Content}");
        Console.WriteLine(citation.FilePath);

        decodedStr = DecodeURL(citation.FilePath);

        /************************************************/
        /**                 Add Citation               **/
        /************************************************/
        if (count == 1)
        {
            answer = answer.Replace("[doc1]", "[" + decodedStr + "]");
        }
        if (count == 2)
        {
            answer = answer.Replace("[doc2]", "[" + decodedStr + "]");
        }
        if (count == 3)
        {
            answer = answer.Replace("[doc3]", "[" + decodedStr + "]");
        }
        count += 1;
    }

    Console.WriteLine(answer);
    return answer;

})
    .WithName("SearchLoans")
    .WithOpenApi();

/************************************************/
/**               Base 64 Decoding             **/
/************************************************/
static string DecodeURL(string path)
{
        bool failed = false;
        string decodedStr;
        byte[] decodedBytes = null;

        try
        {
            decodedBytes = Convert.FromBase64String(path.Remove(path.Length-1));
        }
        catch
        {
            failed = true;
        }

        if (failed)
        try
        {
            failed = false;
            decodedBytes = Convert.FromBase64String(path.Remove(path.Length-1) + "=");
        }
        catch
        {
            failed = true;
        }

        if (failed)
        try
        {
            failed = false;
            decodedBytes = Convert.FromBase64String(path.Remove(path.Length-1) + "==");
        }
        catch
        {
            failed = true;
        }

        if (!failed) 
        {
            decodedStr = System.Text.Encoding.UTF8.GetString(decodedBytes);
        }
        else
        {
            decodedStr = "";
        } 
        return decodedStr;
}
app.Run();