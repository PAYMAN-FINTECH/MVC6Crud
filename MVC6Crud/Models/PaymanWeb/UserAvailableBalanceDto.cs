namespace MVC6Crud.Models.PaymanWeb
{
    public class UserAvailableBalanceDto
    {
        public string UserPhone { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string UserPin { get; set; }
        public string WebPass { get; set; }
        public decimal? PayOut { get; set; }
        public decimal? PayIn { get; set; }
        public decimal? PayIncomm { get; set; }
        public decimal WalletBalance { get; set; }
    }
}
