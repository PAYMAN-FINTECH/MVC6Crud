namespace MVC6Crud.Models.PaymanApp
{
    public class OpenMoneyPaymentResponse
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string payment_error_code { get; set; }
        public string payment_error_description { get; set; }
        public string vpa { get; set; }
        public string sub_accounts_id { get; set; }
        public string id { get; set; }
        public string entity { get; set; }
        public string status { get; set; }
        public PaymentInstrument payment_instrument { get; set; }
        public Customer customer { get; set; }
        public PaymentToken payment_token { get; set; }
    }

    public class PaymentInstrument
    {
        public string entity { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int type_id { get; set; }
        public string type_name { get; set; }
    }

    public class Customer
    {
        public string contact_number { get; set; }
        public string email_id { get; set; }
        public string id { get; set; }
        public string entity { get; set; }
    }

    public class PaymentToken
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string mtx { get; set; }
        public int attempts { get; set; }
        public string sub_accounts_id { get; set; }
        public string id { get; set; }
        public string entity { get; set; }
        public string status { get; set; }
    }
}
