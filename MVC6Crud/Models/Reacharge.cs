using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace MVC6Crud.Models
{
    public class Reacharge
    {
        public List<GetCircle> GetCircle { get; set; }
        public int SelectedGetCircleId { get; set; }

        public List<GetOperator> GetOperator { get; set; }
        public int SelectedGetOperatorId { get; set; }

        public List<GetBalance> GetBalance { get; set; }
        public int SelectedGetBalanceId { get; set; }


        public List<Circle> Data { get; set; }
         public List<Operator> operators { get; set; }
        public string circleDropdown { get; set; }
        public string operatorDropdown { get; set; }
        public string MobileNumber { get; set; }
        public string featchAmountDropdown { get; set; }
    }
}
