namespace MVC6Crud.Models
{
    public class SafexPayOut
    {
        public Guid Id { get; set; }
        public string MobileNo { get; set; }
        public string TxnAmount { get; set; }
        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string TxnType { get; set; }
        public string AccountType { get; set; }
        public string EmailId { get; set; }
        public string OrderRefNo { get; set; }

        public string JSONtext { get; set; }
        public string Key { get; set; }
        public string JsonEncrypted { get; set; } 

        public string SefexAPI { get; set; }
        public string SefexRequest { get; set; }
        public string AgId { get; set; }
        public string SefexResponsePayload { get; set; }
        public string ReqTime { get; set; }
        public string RespTime { get; set; }
        public string RequestId { get; set; }
        public string Rid { get; set; }

        public string SefexDecryptor { get; set; }

        public string BankStatus { get; set; }
    }
}
