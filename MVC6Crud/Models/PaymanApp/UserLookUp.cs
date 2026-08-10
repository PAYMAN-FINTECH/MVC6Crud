namespace MVC6Crud.Models.PaymanApp
{
    public class UserLookUp
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public string UserPhone { get; set; }
        public PayManUsers User { get; set; }

        public Guid GatewayId { get; set; }
        public Gateways Gateway { get; set; }
        public decimal GatewayMargin { get; set; }

        public bool IsEnabled { get; set; } = true;
        
    }
}
