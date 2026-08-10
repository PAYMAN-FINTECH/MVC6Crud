namespace MVC6Crud.Models.PaymanWeb
{
    public class InvoiceViewModel
    {
        public string InvoiceNo { get; set; }
        public string TransactionId { get; set; }
        public string Date { get; set; }
        public PartyInfo From { get; set; }
        public PartyInfo To { get; set; }
        public List<InvoiceLineItem> LineItems { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Total { get; set; }
    }

    public class PartyInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Gstin { get; set; }
    }

    public class InvoiceLineItem
    {
        public int SNo { get; set; }
        public string Service { get; set; }
        public string HsnSac { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }
}
