using System.ComponentModel.DataAnnotations.Schema;

namespace MVC6Crud.Models
{
    public class PayManGateWayMarigin
    {
        public Guid Id { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal Lyra { get; set; }
        [Column(TypeName = "decimal(18,4)")]
        public decimal RazorPay { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal PayManFounds { get; set; }

        public bool SrrEnable { get; set; }
    }
}
