// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentMandate.Models;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using sk_agent.Models;
using AgentMandate.Services;
using AgentsSample;

namespace Plugins;

/// <summary>
/// Represents a plugin for purchasing products.
/// </summary>
internal sealed class PurchasePlugin
{
    private readonly string _token = string.Empty;
    private readonly List<Mandate> _mandates;
    private readonly Settings _settings;
    private readonly bool localMandateValidationRequired = false;

    public PurchasePlugin(string token, List<Mandate> mandates, Settings settings)
    {
        _token = token;
        _mandates = mandates;
        _settings = settings;
    }

    [KernelFunction]
    /// This function is used to purchase a product.
    /// <param name="productName">The name of the product to purchase.</param>
    /// <param name="quantity">The quantity of the product to purchase.</param>
    /// <param name="agentrole">The role of the agent performing the purchase.</param>
    /// <returns>The completion message.</returns>
    public async Task<string> PurchaseProduct(string productName, int quantity)
    {
        if (localMandateValidationRequired)
        {
            var validation = MandateService.ValidateActionAgainstConditions(_mandates, 0, quantity);
            if (!validation.IsValid)
            {
                return validation.Message;
            }
        }
        var result = await Purchase(productName, quantity);

        return $"The purchase of {quantity} item(s) of {productName} completed! {result}";
    }

    [KernelFunction]
    /// This function is used to get a list of prices for a product.
    /// <param name="productName">The name of the product to purchase.</param>
    /// <returns>The completion message.</returns>
    public async Task<List<ProductSpecification>> GetProductPricesAsync(string productName)
    {

        var productSpecifications = await GetProductInformation(productName);

        return productSpecifications;
    }

    private async Task<List<ProductSpecification>> GetProductInformation(string productName)
    {
        using HttpClient client = await CreateHttpClient();

        var response = await client.GetAsync($"/Purchase?productName={productName}");
        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var productSpecifications = JsonSerializer.Deserialize<List<ProductSpecification>>(responseContent, options);

        if (productSpecifications != null)
        {
            return productSpecifications;
        }
        else
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get product information: {response.ReasonPhrase}");
            }
            else
            {
                throw new Exception("Failed to parse product information response.");
            }
        }
    }

    private async Task<string> Purchase(string productName, int quantity)
    {
        using HttpClient client = await CreateHttpClient();
        var mandatesJson = JsonSerializer.Serialize(_mandates);

        // Add mandatesJson as a header
        client.DefaultRequestHeaders.Add("Mandates", mandatesJson);

        var purchaseData = new
        {
            ProductName = productName,
            Quantity = quantity
        };

        var content = new StringContent(JsonSerializer.Serialize(purchaseData), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/Purchase", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var purchaseResponse = JsonSerializer.Deserialize<PurchaseResponse>(responseContent, options);

        if (purchaseResponse != null)
        {
            return $"Purchase ID: {purchaseResponse.PurchaseId}";
        }
        else
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to complete purchase: {response.ReasonPhrase}");
            }
            else
            {
                throw new Exception("Failed to parse purchase response.");
            }
        }
    }

    private async Task<HttpClient> CreateHttpClient()
    {
        // Get an access token for the API
        string[] scopes = _settings.AzureAD.APIScopes.Split(' ');
        var accessToken = await RequestAccessTokenAsync(scopes);
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7108")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<string?> RequestAccessTokenAsync(string[] scopes)
    {
        // Create a ConfidentialClientApplication
        var confidentialClient = ConfidentialClientApplicationBuilder
            .Create(_settings.AzureAD.ClientId)
            .WithClientSecret(_settings.AzureAD.ClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_settings.AzureAD.TenantId}"))
            .Build();

        try
        {
            // Perform On-Behalf-Of flow to get an access token
            var result = await confidentialClient.AcquireTokenOnBehalfOf(scopes, 
                new UserAssertion(_token))
                .ExecuteAsync();

            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // Handle consent request
            Console.WriteLine("User consent is required. Please provide consent interactively.");

            // Create a PublicClientApplication for interactive consent
            var publicClient = PublicClientApplicationBuilder.Create(_settings.AzureAD.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, _settings.AzureAD.TenantId)
                .WithDefaultRedirectUri()
                .Build();

            try
            {
                var result = await publicClient.AcquireTokenInteractive(scopes)
                    .WithAccount((await publicClient.GetAccountsAsync()).FirstOrDefault())
                    .ExecuteAsync();

                return result.AccessToken;
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"Error acquiring token interactively: {ex.Message}");
            }
        }
        catch (MsalServiceException ex)
        {
            // Handle token acquisition failure
            Console.WriteLine($"Error acquiring token: {ex.Message}");
        }

        return null;
    }
}