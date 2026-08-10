using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace MVC6Crud.Models.CashFree
{
    public class CFConfig
    {
        public string ClientId { get; set; } = "1107494346a1efef60b1c005c144947011";
        public string ClientSecret { get; set; } = "cfsk_ma_prod_c93acfbde14f519d42813833aa702639_8a28dd85";
        public string ApiVersion { get; set; } = "2025-01-01";
        public string BaseUrl { get; set; } = "https://api.cashfree.com"; // Production URL

        //public string ClientId { get; set; } = "TEST10848652a1e234e4039aca5640c725684801";
        //public string ClientSecret { get; set; } = "cfsk_ma_test_1aff1aa1971b486f1222a124ab3da1eb_a2cbadf6";
        //public string ApiVersion { get; set; } = "2025-01-01";
        //public string BaseUrl { get; set; } = "https://sandbox.cashfree.com"; // Production URL
    }

    public class CFOrderRequest
    {
        public string order_id { get; set; }
        public decimal order_amount { get; set; }
        public string order_currency { get; set; } = "INR";
        public string order_note { get; set; }
        public CFOrderMeta order_meta { get; set; }
        public CFCustomerDetails customer_details { get; set; }
    }

    public class CFOrderMeta
    {
        public string return_url { get; set; }
        public string notify_url { get; set; }
    }

    public class CFCustomerDetails
    {
        public string customer_id { get; set; }
        public string customer_email { get; set; }
        public string customer_phone { get; set; }
        public string customer_name { get; set; }
    }



    public class CFOrderResponse
    {
        [JsonPropertyName("cart_details")]
        public object cart_details { get; set; }

        [JsonPropertyName("cf_order_id")]
        public string cf_order_id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime created_at { get; set; }

        [JsonPropertyName("customer_details")]
        public CustomerDetails customer_details { get; set; }

        [JsonPropertyName("entity")]
        public string entity { get; set; }

        [JsonPropertyName("order_amount")]
        public double order_amount { get; set; }

        [JsonPropertyName("order_currency")]
        public string order_currency { get; set; }

        [JsonPropertyName("order_expiry_time")]
        public DateTime order_expiry_time { get; set; }

        [JsonPropertyName("order_id")]
        public string order_id { get; set; }

        [JsonPropertyName("order_meta")]
        public OrderMeta order_meta { get; set; }

        [JsonPropertyName("order_note")]
        public string order_note { get; set; }

        [JsonPropertyName("order_splits")]
        public List<object> order_splits { get; set; }

        [JsonPropertyName("order_status")]
        public string order_status { get; set; }

        [JsonPropertyName("order_tags")]
        public object order_tags { get; set; }

        [JsonPropertyName("payment_session_id")]
        public string payment_session_id { get; set; }

        [JsonPropertyName("products")]
        public Products products { get; set; }

        [JsonPropertyName("terminal_data")]
        public object terminal_data { get; set; }
    }

    public class CustomerDetails
    {
        [JsonPropertyName("customer_id")]
        public string customer_id { get; set; }

        [JsonPropertyName("customer_name")]
        public string customer_name { get; set; }

        [JsonPropertyName("customer_email")]
        public string customer_email { get; set; }

        [JsonPropertyName("customer_phone")]
        public string customer_phone { get; set; }

        [JsonPropertyName("customer_uid")]
        public string customer_uid { get; set; }
    }

    public class OrderMeta
    {
        [JsonPropertyName("notify_url")]
        public string notify_url { get; set; }

        [JsonPropertyName("payment_methods")]
        public object payment_methods { get; set; }

        [JsonPropertyName("payment_methods_filters")]
        public object payment_methods_filters { get; set; }

        [JsonPropertyName("return_url")]
        public string return_url { get; set; }
    }

    public class Products
    {
        [JsonPropertyName("one_click_checkout")]
        public OneClickCheckout one_click_checkout { get; set; }

        [JsonPropertyName("verify_pay")]
        public VerifyPay verify_pay { get; set; }
    }

    public class OneClickCheckout
    {
        [JsonPropertyName("enabled")]
        public bool enabled { get; set; }

        [JsonPropertyName("conditions")]
        public List<object> conditions { get; set; }
    }

    public class VerifyPay
    {
        [JsonPropertyName("enabled")]
        public bool enabled { get; set; }

        [JsonPropertyName("conditions")]
        public List<object> conditions { get; set; }
    }


    public class InternationalPayment
    {
        public bool international { get; set; }
    }

    public class Card
    {
        public string card_bank_name { get; set; }
        public string card_country { get; set; }
        public string card_network { get; set; }
        public string card_network_reference_id { get; set; }
        public string card_number { get; set; }
        public string card_sub_type { get; set; }
        public string card_type { get; set; }
        public string channel { get; set; }
        public string instrument_id { get; set; }
    }

    public class PaymentMethod
    {
        public Card card { get; set; }
    }

    public class PaymentGatewayDetails
    {
        public string gateway_name { get; set; }
        public string gateway_order_id { get; set; }
        public string gateway_payment_id { get; set; }
        public string gateway_order_reference_id { get; set; }
        public string gateway_status_code { get; set; }
        public string gateway_settlement { get; set; }
        public string gateway_reference_name { get; set; }
    }

    public class PaymentSurcharge
    {
        public decimal payment_surcharge_service_charge { get; set; }
        public decimal payment_surcharge_service_tax { get; set; }
    }

    public class PaymentCashfree
    {
        public string auth_id { get; set; }
        public string authorization { get; set; }
        public string bank_reference { get; set; }
        public string cf_payment_id { get; set; }
        public string entity { get; set; }
        public object error_details { get; set; }
        public InternationalPayment international_payment { get; set; }
        public bool is_captured { get; set; }
        public decimal order_amount { get; set; }
        public string order_currency { get; set; }
        public string order_id { get; set; }
        public decimal payment_amount { get; set; }
        public DateTime payment_completion_time { get; set; }
        public string payment_currency { get; set; }
        public PaymentGatewayDetails payment_gateway_details { get; set; }
        public string payment_group { get; set; }
        public string payment_message { get; set; }
        public PaymentMethod payment_method { get; set; }
        public object payment_offers { get; set; }
        public string payment_status { get; set; }
        public PaymentSurcharge payment_surcharge { get; set; }
        public DateTime payment_time { get; set; }
    }

    public class Root
    {
        public List<Payment> Payments { get; set; }
    }


}
