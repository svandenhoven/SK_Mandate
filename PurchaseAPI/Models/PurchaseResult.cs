namespace PurchaseAPI.Models
{
    public class PurchaseResult
    {
        public bool IsSuccessFullPurchase { get; set; }
        public required string ReturnMessage { get; set; }
    }
}
