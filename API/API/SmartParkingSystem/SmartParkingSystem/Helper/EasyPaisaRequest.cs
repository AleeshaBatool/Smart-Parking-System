namespace SmartParkingSystem.Helper
{
    public class EasyPaisaRequest
    {
        public string orderId { get; set; }
        public string storeId { get; set; } = "496119"; // your store ID
        public string transactionAmount { get; set; }
        public string transactionType { get; set; } = "MA";
        public string mobileAccountNo { get; set; }
        public string emailAddress { get; set; }
    }
}
