namespace MVC6Crud.Models.PaymanApp
{
    public class BillAvenueCreditCardBillers
    {
        public Guid Id { get; set; }
        public string blr_id { get; set; }
        public string blr_name { get; set; }
        public string? blr_alias_name { get; set; }
        public string? blr_category_name { get; set; }
        public string? blr_coverage { get; set; }
        public string? blr_response { get; set; }
        public DateTime InsertedDate { get; set; }
        public bool IsActive { get; set; }
        public string? PaymentModes { get; set; }
        public string? iconUrl { get; set; }
    }
}
