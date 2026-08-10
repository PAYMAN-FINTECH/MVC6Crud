namespace MVC6Crud.Models.PaymanWeb
{
    public class DashboardViewModelWeb
    {
        public List<CarouselItem> CarouselData { get; set; }
        public string UserName { get; set; }
        public string UserPhone { get; set; }
        public string Email { get; set; }
        public bool IsAdmin {  get; set; } 
        public string ProfileImageUrl { get; set; }
        public List<PaymentToggle> PaymentToggles { get; set; }
        public List<BalanceCard> BalanceCards { get; set; }
        public List<ServiceItem> Services { get; set; }
        public string BottomBannerImageUrl { get; set; }
        public bool? PayOutEnable { get; set; }
        public bool? CCBill {  get; set; }
        public bool? PayIn {  get; set; }
        public string CustomerType { get; set; }
        public bool? OpenMoney { get; set; }
        public bool? EduCashfree { get; set; }
        public bool? EduGateway { get; set; }
        public bool? Easebuzz1 { get; set; }
        public bool? Easebuzz2 { get; set; }
        public bool? RozorpayEdu { get; set; }
        public bool? UserEasebuzz { get; set; }
        
    }
    public class CarouselItem
    {
        public string Image { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class PaymentToggle
    {
        public string Label { get; set; }
        public bool IsChecked { get; set; }
    }

    public class BalanceCard
    {
        public string Label { get; set; }
        public string Amount { get; set; }
        public string IconSvg { get; set; }
        public bool IsHighlighted { get; set; }
    }

    public class ServiceItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconClass { get; set; }
    }
}
