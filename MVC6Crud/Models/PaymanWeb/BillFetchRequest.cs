namespace MVC6Crud.Models.PaymanWeb
{
    public class BillFetchRequest
    {
        public string BillerId { get; set; }
        public string Last4Digits { get; set; }
        public string CardHolderMobile { get; set; }
        public string CustomerMobile { get; set; }
    }
}
