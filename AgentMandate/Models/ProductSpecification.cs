using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentMandate.Models
{
    /// <summary>
    /// Represents the specification of a product.
    /// </summary>
    public class ProductSpecification
    {
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public decimal Price { get; set; }

        public string Discount { get; set; }
        
    }
}
