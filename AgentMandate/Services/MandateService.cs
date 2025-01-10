﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgentMandate.Models;

namespace AgentMandate.Services
{
    public class MandateService
    {
        public static List<Mandate> GetMandates(string userId)
        {
            // Below code is for reference only, in real life the object would be retrieve from a Mandate Management Solution
            var mandate = new Mandate
            {
                MandateId = Guid.NewGuid(),
                Action = "Purchase",
                GrantedByUserId = userId, // User ID of the person granting the mandate
                ValidFrom = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(30), // Mandate expires in 30 days
                Conditions = new List<Condition>
                {
                    new Condition
                    {
                        ConditionId = Guid.NewGuid(),
                        Type = ConditionTypes.MaxPrice,
                        Value = 100, // Maximum price is $100
                        Unit = ConditionUnits.EUR,
                        Operator = ConditionOperators.LessThanOrEqual
                    },
                    new Condition
                    {
                        ConditionId = Guid.NewGuid(),
                        Type = ConditionTypes.MaxQuantity,
                        Value = 20, // Maximum quantity is 20 units
                        Unit = ConditionUnits.Items,
                        Operator = ConditionOperators.LessThanOrEqual
                    }
                }
            };

            return [mandate];
        }
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
                            Message = "Price exceeds the maximum allowed price."
                        };
                    }

                    // Check quantity
                    if (condition.Type == ConditionTypes.MaxQuantity && condition.Operator == ConditionOperators.LessThanOrEqual && quantity > condition.Value)
                    {
                        return new MandateValidationResult
                        {
                            IsValid = false,
                            Message = "Quantity exceeds the maximum allowed quantity."
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
