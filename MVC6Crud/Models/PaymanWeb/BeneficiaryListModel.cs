namespace MVC6Crud.Models.PaymanWeb
{
    public class BeneficiaryListModel
    {
        public string Phone {  get; set; }
        public string Pinlabamount { get; set; }
        public string UserAvlAmount { get; set; }
        public int? PayOutMinAmount { get; set; }
        public int? PayOutMaxAmount { get; set; }
        public int? MinBalanceAvl { get; set; }

        public int? InstatntPay { get; set; }
        public string? CustomerType { get; set; }
        public bool isFastTag { get; set; }
        public string billerName { get; set; }
        public bool? isCsbBankPayout { get; set; }

    }
}
