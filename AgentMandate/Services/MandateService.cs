using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgentMandate.Models;

namespace AgentMandate.Services
{
    public class MandateService
    {
        /// <summary>
        /// Retrieves the mandates for a given user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<Mandate> GetMandates(string userId)
        {
            // Below code is for reference only, in real life the object would be retrieve from a Mandate Management Solution
            var mandate = new Mandate
            {
                MandateId = Guid.NewGuid(),
                Action = "Purchase",
                GrantedByUserId = userId, // User ID of the person granting the mandate
                ValidFrom = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(2), // Mandate expiry
                Conditions = new List<Condition>
                {
                    new Condition
                    {
                        ConditionId = Guid.NewGuid(),
                        Type = ConditionTypes.MaxPrice,
                        Value = 150, // Maximum price
                        Unit = ConditionUnits.USD,
                        Operator = ConditionOperators.LessThanOrEqual
                    },
                    new Condition
                    {
                        ConditionId = Guid.NewGuid(),
                        Type = ConditionTypes.MaxQuantity,
                        Value = 100, // Maximum quantity
                        Unit = ConditionUnits.Items,
                        Operator = ConditionOperators.LessThanOrEqual
                    }
                }
            };

            return [mandate];
        }

        /// <summary>
        /// Validates the action against the conditions of the mandate.
        /// </summary>
        /// <param name="mandates"></param>
        /// <param name="price"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public static MandateValidationResult ValidateActionAgainstConditions(List<Mandate> mandates, decimal price, int quantity)
        {
            foreach (var mandate in mandates)
            {
                foreach (var condition in mandate.Conditions)
                {
                    // Check price
                    if (condition.Type == ConditionTypes.MaxPrice && condition.Operator == ConditionOperators.LessThanOrEqual && price > condition.Value)
                    {
                        return new MandateValidationResult
                        {
                            IsValid = false,
                            Message = $"Price exceeds the maximum allowed price of {condition.Value}."
                        };
                    }

                    // Check quantity
                    if (condition.Type == ConditionTypes.MaxQuantity && condition.Operator == ConditionOperators.LessThanOrEqual && quantity > condition.Value)
                    {
                        return new MandateValidationResult
                        {
                            IsValid = false,
                            Message = $"Quantity exceeds the maximum allowed quantity of {condition.Value}."
                        };
                    }

                }
            }
            return new MandateValidationResult
            {
                IsValid = true,
                Message = "Mandate conditions validated."
            };
        }
    }
}
