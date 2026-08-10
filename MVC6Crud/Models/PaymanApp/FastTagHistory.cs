namespace MVC6Crud.Models.PaymanApp
{
    public class FastTagHistory
    {
        
        public decimal? Amount { get; set; }
        public string Status { get; set; }

        public string RefId { get; set; }
        public string AccountHolderName {  get; set; }
        public string AccountNo {  get; set; }
        public DateTime? DateTime { get; set; }
    }
}
