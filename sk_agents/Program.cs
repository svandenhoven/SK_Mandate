// See https://aka.ms/new-console-template for more information
using AgentMandate.Services;
using AgentsSample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Plugins;

// Get the settings
Settings settings = new Settings();

// Authenticate the user
Console.WriteLine("Authenticating user...");
string clientId = settings.AzureAD.ClientId;
string tenantId = settings.AzureAD.TenantId;
string[] scopes = settings.AzureAD.Scopes.Split(" ");

var app = PublicClientApplicationBuilder.Create(clientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
    .WithDefaultRedirectUri()
    .Build();

AuthenticationResult result;
try
{
    var accounts = await app.GetAccountsAsync();
    result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
        .ExecuteAsync();
}
catch (MsalUiRequiredException)
{
    result = await app.AcquireTokenInteractive(scopes)
        .ExecuteAsync();
}

// Get users Mandates for agent
Console.WriteLine("Getting user mandates...");
var mandates = MandateService.GetMandates(result.Account.HomeAccountId.Identifier);

Console.WriteLine("Initialize plugins...");
PurchasePlugin purchasePlugin = new(result.AccessToken, mandates, settings);  

Console.WriteLine("Creating kernel...");
IKernelBuilder builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion(
    settings.AzureOpenAI.ChatModelDeployment,
    settings.AzureOpenAI.Endpoint,
    settings.AzureOpenAI.ApiKey);
builder.Plugins.AddFromObject(purchasePlugin);

// Enable Invocation Filter by uncomment below line
// builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();

Kernel kernel = builder.Build();

// Add the access token to the kernel
kernel.Data.Add("idtoken", result.IdToken);

Console.WriteLine($"User signed in: {result.Account.Username}");

// Define the agent
Console.WriteLine("Defining agent...");
ChatCompletionAgent agent =
    new()
    {
        Name = "SampleAssistantAgent",
        Instructions =
            """
            You are an agent that has role {{$agentrole}} and is designed to purchase products. 
            The product name and the quantity are required to complete the purchase.
            Before purchasing the product, the agent must get the product specifications and prices. 
            The agent must choose the product that is cheapest and choose that one to purchase.

            When a product from manufacturer is chosen, always mention what product is chosen for which price and from which manufaturer. 
            Always mention the prices from other manufacturer.
            Prices should be in USD and with 2 decimal places.
            When purchase is successful, also mention in the answer the date of purchase, which is {{$now}} and the Purchase ID.
            """,
        Kernel = kernel,
        Arguments =
            new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
    };



// Add the mandates to the agent
kernel.Data.Add("mandates", mandates);

Console.WriteLine("Ready!");

ChatHistory history = [];
bool isComplete = false;
do
{
    Console.WriteLine();
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }
    if (input.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    {
        isComplete = true;
        break;
    }
    if (input.Trim().Equals("StartOver", StringComparison.OrdinalIgnoreCase))
    {
        history.Clear();
    }

    history.Add(new ChatMessageContent(AuthorRole.User, input));

    Console.WriteLine();

    DateTime now = DateTime.Now;
    KernelArguments arguments =
    new()
        {
            { "now", $"{now.ToShortDateString()} {now.ToShortTimeString()}" },
            { "agentid", "abcde" },
            { "agentrole", "purchaseAgent" }
        };
    await foreach (ChatMessageContent response in agent.InvokeAsync(history, arguments))
    {
        Console.WriteLine($"{response.Content}");
    }
} while (!isComplete);