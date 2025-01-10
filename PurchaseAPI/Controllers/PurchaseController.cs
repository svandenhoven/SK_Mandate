using AgentMandate.Models;
using AgentMandate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Text.Json;

namespace PurchaseAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class PurchaseController : ControllerBase
    {
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(ILogger<PurchaseController> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "Purchase")]
        public IActionResult Post([FromBody] Product product)
        {
            //Get mandate from request headers
            var mandatesHeader = Request.Headers["Mandates"];
            if (mandatesHeader.Count == 0)
            {
                return Unauthorized("Agent mandates are required to purchase a product");
            }  

            var mandates = JsonSerializer.Deserialize<List<Mandate>>(mandatesHeader[0]!);
            var mandateValidation = MandateService.ValidateActionAgainstConditions(mandates, 0, product.Quantity);
            if (!mandateValidation.IsValid)
            {
                return Unauthorized(mandateValidation.Message);
            }
            // Purchase the product
            // ToDo: Purchase product
            _logger.LogInformation($"Purchased {product.Quantity} {product.ProductName}(s)");
            var purchaseId = Guid.NewGuid();
            return Ok(new { PurchaseId = purchaseId });
        }

        [HttpGet(Name = "GetProductInformation")]
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
                },
                new ProductSpecification
                {
                    Name = productName,
                    Manufacturer = "Fruitopia Market",
                    Price = (decimal) (basePrice + random.NextDouble()),
                },
                new ProductSpecification
                {
                    Name = productName,
                    Manufacturer = "The Juicy Orchard",
                    Price = (decimal) (basePrice + random.NextDouble()),
                }
            }; 

            return Ok(productSpecifications);
        }
    }
}