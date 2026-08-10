namespace MVC6Crud.Models
{
    public class SendtoAirpayModel
    {
        // Buyer Information
        public string BuyerEmail { get; set; }
        public string BuyerPhone { get; set; }
        public string BuyerFirstName { get; set; }
        public string BuyerLastName { get; set; }
        public string BuyerAddress { get; set; }
        public string BuyerCity { get; set; }
        public string BuyerState { get; set; }
        public string BuyerCountry { get; set; }
        public string BuyerPinCode {  get; set; }

        // Payment Information
        public string MerchantId { get; set; } // Corresponds to 'mercid'
        public string OrderId { get; set; } // Corresponds to 'orderid'
        public string Currency { get; set; } // Corresponds to 'currency'
        public string IsoCurrency { get; set; } // Corresponds to 'isocurrency'
        public string Amount { get; set; }

        // Transaction Details
        public string Txnsubtype { get; set; }
        public string CustomVar { get; set; }
        public string TxnSubtype { get; set; }

        // Security Details
        public string PrivateKey { get; set; }
        public string Checksum { get; set; }
    }

}
