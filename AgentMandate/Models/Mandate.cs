using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentMandate.Models
{
    public class MandateValidationResult
    {
        public bool IsValid { get; set; } // Whether the action is valid
        public string Message { get; set; } // Additional information or error message
    }   

    public class Mandate
    {
        public Guid MandateId { get; set; } // Unique identifier for the mandate
        public string Action { get; set; } // The specific action this mandate authorizes (e.g., "Purchase")
        public string GrantedByUserId { get; set; } // User who granted the mandate
        public DateTime ValidFrom { get; set; } // Start of the mandate's validity
        public DateTime? ValidUntil { get; set; } // End of the mandate's validity (optional)
        public List<Condition> Conditions { get; set; } = new(); // List of conditions defining the mandate's limits
    }


    public class Condition
    {
        public Guid ConditionId { get; set; } // Unique identifier for the condition
        public ConditionTypes Type { get; set; } // Key or name of the condition (e.g., "MaxPrice", "MaxQuantity")
        public decimal Value { get; set; } // Value for the condition (e.g., "1000" for MaxPrice)
        public ConditionUnits Unit { get; set; } // Unit of measurement for the value (e.g., "USD", "EUR")
        public ConditionOperators Operator { get; set; } // Comparison operator (e.g., "LessThan", "Equals")
    }

    public class ActionLog
    {
        public Guid ActionLogId { get; set; } // Unique identifier for the action log
        public Guid AgentId { get; set; } // Agent performing the action
        public Guid MandateId { get; set; } // Mandate under which the action was performed
        public string Action { get; set; } // The action performed (e.g., "Purchase")
        public DateTime Timestamp { get; set; } // When the action was performed
        public bool WasSuccessful { get; set; } // Whether the action was successful
        public string Remarks { get; set; } // Additional details or comments
    }
}
