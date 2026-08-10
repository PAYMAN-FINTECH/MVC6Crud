namespace MVC6Crud.Models.PaymanApp
{
    public class PayManGateways
    {
        public Guid Id { get; set; }
        public string? GatewayName { get; set; }
        public decimal? PayInComm { get; set; }
        public decimal? PayOutComm { get; set; }
        public decimal? PaymanComm { get; set; }
        public DateTime? Created { get; set; }
        public bool? Easebuzz1 { get; set; }
        public bool? Easebuzz2 { get; set; }
        public bool? RazorPay { get; set; }
        public int? EasebuzzGatewayEnableAmount { get; set; }
        public int? PayoutCount { get; set; }
        public int? PayOutMinAmount { get; set; }
        public int? PayOutMaxAmount { get; set; }
        public int? MinBalanceAvl { get; set; }
        public bool? PayIn { get; set; }
        public bool? PayOut { get; set; }
        public bool? CcBill { get; set; }
        public bool? InstantPay { get; set; }
        public bool? BillAvenue { get; set; }
        public bool? Edu { get; set; }
        public int? EduEnableAmount { get; set; }
        public bool? CsbbankPayout { get; set; }
        public bool? PineLabPayout { get; set; }
    }
    public class Gateways
    {
        public Guid Id { get; set; }
        public string GatewayName { get; set; }
        public bool IsActive { get; set; }
        public bool Web { get; set; }
        public bool App { get; set; }
        public decimal GatwayLimit { get; set; }
        public string? StoreName { get; set; }
    }
}
