using AgentMandate.Models;
using AgentMandate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Text.Json;
using PurchaseAPI.Services;
using PurchaseAPI.Models;

namespace PurchaseAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PurchaseController : ControllerBase
    {
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(ILogger<PurchaseController> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "Purchase")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes:Purchase")]
        // To avoid that API is mistakenly executed as user when no act_as scope is present, we require at least one of the scopes
        [RequiredAnyScope(ActAsScopes.AgentScope, ActAsScopes.UserScope)]
        public IActionResult Post([FromBody] Product product)
        {
            var scopes = GetScopesFromToken();

            // If the accesstoken defines the agent_scope, then the caller is acting as an agent
            // This requires the caller to provide mandates in the request headers
            // The mandates are validated against the conditions of the mandate
            if (scopes.Contains(ActAsScopes.AgentScope))
            {
                //Get mandate from request headers
                var mandatesHeader = Request.Headers["Mandates"];
                if (mandatesHeader.Count == 0)
                {
                    return Unauthorized("Agent mandates are required to purchase a product as an agent.");
                }

                List<Mandate>? mandates;
                try
                {
                    mandates = JsonSerializer.Deserialize<List<Mandate>>(mandatesHeader[0]!);
                }
                catch (JsonException)
                {
                    return Unauthorized("Invalid mandates format.");
                }

                var mandateValidation = MandateService.ValidateActionAgainstConditions(mandates!, 0, product.Quantity);
                if (!mandateValidation.IsValid)
                {
                    return Unauthorized(mandateValidation.Message);
                }
            }

            // ToDo: Actually Purchase the product
            _logger.LogInformation($"Purchased {product.Quantity} {product.ProductName}(s)");
            var purchaseId = Guid.NewGuid();
            return Ok(new { PurchaseId = purchaseId });
        }

        [HttpGet(Name = "GetProductInformation")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes:Prices")]
        public IActionResult Get(string productName)
        {
            Random random = new Random();
            var basePrice = random.NextDouble() * 3;
            var productSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification
                        {
                            Name = productName,
                            Manufacturer = "King Fruits",
                            Price = (decimal) (basePrice + random.NextDouble()),
                            Discount = "5% Discount for purchases between 15 and 25 items. 10% Discount for purchases between 25-70 items. 20% Discount for purchases for more."
                        },
                        new ProductSpecification
                        {
                            Name = productName,
                            Manufacturer = "Fruitopia Market",
                            Price = (decimal) (basePrice + random.NextDouble()),
                            Discount = "6% Discount for purchases between 10 and 20 items. 15% Discount for purchases between 20-40 items. 15% Discount for purchases for more."
                        },
                        new ProductSpecification
                        {
                            Name = productName,
                            Manufacturer = "The Juicy Orchard",
                            Price = (decimal) (basePrice + random.NextDouble()),
                            Discount = "1% Discount for purchases between 15 and 25 items. 15% Discount for purchases between 25-55 items. 35% Discount for purchases for more."
                        }
                    };

            return Ok(productSpecifications);
        }

        private IEnumerable<string> GetScopesFromToken()
        {
            var scopeClaim = HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
            if (scopeClaim != null)
            {
                return scopeClaim.Value.Split(' ');
            }
            return Enumerable.Empty<string>();
        }
    }
}