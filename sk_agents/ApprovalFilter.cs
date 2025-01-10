using AgentMandate.Models;
using AgentMandate.Services;
using Microsoft.SemanticKernel;

public class ApprovalFilter() : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        List<Mandate>? mandates = context.Kernel.Data["mandates"] as List<Mandate>;

        if (mandates != null && context.Function.PluginName == "PurchasePlugin" && context.Function.Name == "PurchaseProduct")
        {
            context.Arguments.TryGetValue("productName", out object? productName);
            context.Arguments.TryGetValue("quantity", out object? quantity);

            if (!MandateService.ValidateActionAgainstConditions(mandates, 0, Convert.ToInt16(quantity)).IsValid)
            {
                Console.WriteLine($"System > The agent want to purchase {quantity} {productName}, do you want to proceed? (Y/N)");
                Console.Write("> ");
                string shouldProceed = Console.ReadLine()!;

                if (shouldProceed.ToLower() != "y" && shouldProceed.ToLower() != "yes")
                {
                    context.Result = new FunctionResult(context.Result, "The purchase was not approved by the user");
                    return;
                }
            }
        }
        await next(context);
    }
}
