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
//builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();

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
            You are an agent with the role {{$agentrole}}, designed to purchase products efficiently.

            Task:
            1. Product Information:
               - Obtain the product name, specifications, quantity, and all pricing details (including discounts) from manufacturers.

            2. Price Optimization:
               - Account for discounts and identify the lowest total price by comparing offers from all manufacturers.
               - If necessary, split the purchase into multiple transactions to achieve the minimum price.
               - Specify quantities, unit prices, and discounts for each purchase and explain the rationale for your choices.
               - Clearly justify why other options are more expensive.

            3. Manufacturer Comparison and Decision:
               - Provide a detailed comparison of prices and discounts for each manufacturer.
               - Explain the decision-making process for selecting a specific manufacturer, including:
                 - All pricing and discount details from each manufacturer (in USD, to 2 decimal places).
                 - The reasoning for choosing the selected manufacturer over the others.
                 - Why the other options were not chosen, including a detailed cost comparison.

            4. Purchase Decision and Execution:
               - Choose the product with the lowest total price and specify the manufacturer.
               - Proceed with the purchase of the chosen product.
               - Ensure all quantities are correctly ordered and all pricing terms are applied as agreed.

            5. Detailed Report:
               - Generate a report suitable for bookkeeping and auditing, including:
                 - Product name, specifications, quantity, and unit price.
                 - Manufacturer details including, item price, discount and total cost (after discounts).
                 - Breakdown of any split purchases (quantities, prices, discounts).
                 - A full comparison of all manufacturers, including item prices and discounts.
                 - A clear explanation of the decision-making process for selecting the manufacturer.
                 - Date of purchase ({{$now}}) and a unique Purchase ID.

            Additional Requirements:
            - Always mention which product was chosen, the price, and the manufacturer.
            - Include all prices and discounts for all manufacturers in the report.
            - Provide a detailed explanation of the decision-making process to ensure transparency for bookkeeping and auditing.       
            """,
        Kernel = kernel,
        Arguments =
            new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
    };


// Add the mandates to the agent, this is used when invocationfilter is used
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
    try
    {
        await foreach (ChatMessageContent response in agent.InvokeAsync(history, arguments))
        {
            Console.WriteLine($"{response.Content}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error occured: {ex.Message}");
    }
} while (!isComplete);