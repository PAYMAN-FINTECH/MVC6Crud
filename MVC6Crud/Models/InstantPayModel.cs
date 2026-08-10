using System.Text.Json.Serialization;

namespace MVC6Crud.Models
{
    public class InstantPayModel
    {
    }
    public class BillerResponse
    {
        public string Statuscode { get; set; }
        public string Actcode { get; set; }
        public string Status { get; set; }
        public BillerData Data { get; set; }
        public string Timestamp { get; set; }
        public string Ipay_uuid { get; set; }
        public string Orderid { get; set; }
        public string Environment { get; set; }
        public string InternalCode { get; set; }
    }

    public class BillerData
    {
        public MetaData Meta { get; set; }
        public List<BillerRecord> Records { get; set; }
    }

    public class MetaData
    {
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsOnCurrentPage { get; set; }
        public int RecordFrom { get; set; }
        public int RecordTo { get; set; }
    }

    public class BillerRecord
    {
        public string BillerId { get; set; }
        public string BillerName { get; set; }
        public string CategoryKey { get; set; }
        public string Type { get; set; }
        public string CategoryName { get; set; }
        public string CoverageCity { get; set; }
        public string CoverageState { get; set; }
        public int CoveragePincode { get; set; }
        public string UpdatedDate { get; set; }
        public string BillerStatus { get; set; }
        public bool IsAvailable { get; set; }
        public string IconUrl { get; set; }
        public string userPhone { get; set; }
    }

    public class BillerCategoryResponse
    {
        public string Statuscode { get; set; }
        public string Status { get; set; }
        public List<BillerCategory> Data { get; set; }
        public string Timestamp { get; set; }
        public string IpayUuid { get; set; }
    }

    public class BillerCategory
    {
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
        public string IconUrl { get; set; }
        public int BillerList { get; set; }
    }

    public class BillerViewModel
    {
        public List<BillerRecord> Billers { get; set; }
        public string SearchTerm { get; set; }
        public decimal AvlAmount { get; set; }
        public string InstanPaYAvlBal { get; set; }
        public string UserName { get; set; }
    }

    // Response Model
    public class BillResponse
    {
        public string DueAmount { get; set; }
        public string DueDate { get; set; }
        public string CustomerName { get; set; }
        public string CardType { get; set; }
        public string EnquiryReferenceId { get; set; }

        public bool Success { get; set; }
        public string Status { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Wallet { get; set; }
    }
    public class CustomerParamDetail
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class AdditionalDetail
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Data
    {
        public string EnquiryReferenceId { get; set; }
        public string CustomerName { get; set; }
        public string BillNumber { get; set; }
        public string BillPeriod { get; set; }
        public string BillDate { get; set; }
        public string BillDueDate { get; set; }
        public string BillAmount { get; set; }
        public List<CustomerParamDetail> CustomerParamsDetails { get; set; }
        public List<object> BillDetails { get; set; }
        public List<AdditionalDetail> AdditionalDetails { get; set; }
    }
    public class PaymentResponseProcess
    {
        public bool Success { get; set; }
        public string Amount { get; set; }
        public string OrderId { get; set; }
        public string ReferenceId { get; set; }
        public string Category { get; set; }
        public string BillerName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string UserPhone {  get; set; }
        public string UserName { get; set; }
    }



    // Your model for the BillRequest
    public class BillRequest
    {
        public string LastFourDigits { get; set; }
        public string Mobile { get; set; }
        public string CustomerMobile { get; set; }
        public string BillerId { get; set; } // BillerId added to model
        public decimal Amount { get; set; }
        public string EnquiryReferenceId { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Wallet { get; set; }
    }

    // Model for Payment Request
    public class PaymentRequest
    {
        public string LastFourDigits { get; set; }
        public string Mobile { get; set; }
        public string CustomerMobile { get; set; }
        public string BillerId { get; set; }
        public decimal Amount { get; set; }
    }

    // Model for API Response
    //public class PaymentResponse1
    //{
    //    public bool Success { get; set; }
    //    public decimal Amount { get; set; }
    //    public string OrderId { get; set; }
    //    public string ReferenceId { get; set; }
    //    public string Category { get; set; }
    //    public string BillerName { get; set; }
    //}

    public class BalanceResponse
    {
        [JsonPropertyName("statuscode")]
        public string StatusCode { get; set; }

        [JsonPropertyName("actcode")]
        public string ActCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public BalanceData Data { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("ipay_uuid")]
        public string IpayUuid { get; set; }

        [JsonPropertyName("orderid")]
        public string OrderId { get; set; }

        [JsonPropertyName("environment")]
        public string Environment { get; set; }

        [JsonPropertyName("internalCode")]
        public string InternalCode { get; set; }
    }

    public class BalanceData
    {
        [JsonPropertyName("bankId")]
        public int BankId { get; set; }

        [JsonPropertyName("bankProfileId")]
        public string BankProfileId { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("accountShortNumber")]
        public string AccountShortNumber { get; set; }

        [JsonPropertyName("balance")]
        public BalanceDetails Balance { get; set; }

        [JsonPropertyName("poolReferenceId")]
        public string PoolReferenceId { get; set; }

        [JsonPropertyName("pool")]
        public PoolDetails1 Pool { get; set; }
    }

    public class BalanceDetails
    {
        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("lien")]
        public string Lien { get; set; }

        [JsonPropertyName("available")]
        public string Available { get; set; }
    }

    public class PoolDetails1
    {
        [JsonPropertyName("account")]
        public string Account { get; set; }

        [JsonPropertyName("openingBal")]
        public string OpeningBalance { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("closingBal")]
        public string ClosingBalance { get; set; }
    }

    public class TransactionResponse
    {
        public string StatusCode { get; set; }
        public string ActCode { get; set; }
        public string Status { get; set; }
        public TransactionData Data { get; set; }
        public string Timestamp { get; set; }
        public string IpayUuid { get; set; }
        public string OrderId { get; set; }
        public string Environment { get; set; }
    }

    public class TransactionData
    {
        public string ExternalRef { get; set; }
        public string PoolReferenceId { get; set; }
        public string TxnValue { get; set; }
        public string TxnReferenceId { get; set; }
        public PoolDetails Pool { get; set; }
        public BillerDetails BillerDetails { get; set; }
        public BillDetails BillDetails { get; set; }
    }

    public class PoolDetails
    {
        public string Account { get; set; }
        public string OpeningBal { get; set; }
        public string Mode { get; set; }
        public string Amount { get; set; }
        public string ClosingBal { get; set; }
    }

    public class BillerDetails
    {
        public string Name { get; set; }
        public string Account { get; set; }
    }

    public class BillDetails
    {
        public string CustomerName { get; set; }
        public string BillNumber { get; set; }
        public string BillPeriod { get; set; }
        public string BillDate { get; set; }
        public string BillDueDate { get; set; }
        public string BillAmount { get; set; }
        public List<CustomerParamDetail> CustomerParamsDetails { get; set; }
        public string AdditionalDetails { get; set; }
    }


    public class BillResponse1
    {
        public string StatusCode { get; set; }
        public string ActCode { get; set; }
        public string Status { get; set; }
        public BillData1 Data { get; set; }
        public string Timestamp { get; set; }
        public string IpayUuid { get; set; }
        public string OrderId { get; set; }
        public string Environment { get; set; }
        public string InternalCode { get; set; }
    }

    public class BillData1
    {
        public string EnquiryReferenceId { get; set; }
        public string CustomerName { get; set; }
        public string BillNumber { get; set; }
        public string BillPeriod { get; set; }
        public string BillDate { get; set; }
        public string BillDueDate { get; set; }
        public string BillAmount { get; set; }
        public List<CustomerParamDetail1> CustomerParamsDetails { get; set; }
        public List<BillDetail1> BillDetails { get; set; }
        public List<AdditionalDetail1> AdditionalDetails { get; set; }
    }

    public class CustomerParamDetail1
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class BillDetail1
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class AdditionalDetail1
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }



    //kjjkjkjl

    public class BillerResponse11
    {
        public string StatusCode { get; set; }
        public string ActCode { get; set; }
        public string Status { get; set; }
        public BillerData11 Data { get; set; }
        public string Timestamp { get; set; }
        public string IpayUuid { get; set; }
        public string OrderId { get; set; }
        public string Environment { get; set; }
        public string InternalCode { get; set; }
    }

    public class BillerData11
    {
        public string BillerId { get; set; }
        public string Mode { get; set; }
        public string AcceptsAdhoc { get; set; }
        public string PaymentAmountExactness { get; set; }
        public string FetchRequirement { get; set; }
        public string SupportValidation { get; set; }
        public BillerInfo BillerInfo { get; set; }
        public Category11 Category { get; set; }
        public Address RegisteredAddress { get; set; }
        public Address CommunicationAddress { get; set; }
        public Coverage Coverage { get; set; }
        public List<InitChannel> InitChannels { get; set; }
        public List<PaymentMode> PaymentModes { get; set; }
        public List<Parameter> Parameters { get; set; }
        public int Timeout { get; set; }
    }

    public class BillerInfo
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Ownership { get; set; }
        public int EffectFrom { get; set; }
        public int EffectTo { get; set; }
    }

    public class Category11
    {
        public string Key { get; set; }
        public string Name { get; set; }
    }

    public class Address
    {
        public string AddressDetail { get; set; }
        public int City { get; set; }
        public int State { get; set; }
        public int Pincode { get; set; }
        public string Country { get; set; }
    }

    public class Coverage
    {
        public string Address { get; set; }
        public string CountryId { get; set; }
        public string CountryCode { get; set; }
        public string Country { get; set; }
        public int StateId { get; set; }
        public string StateCode { get; set; }
        public string State { get; set; }
        public int CityId { get; set; }
        public string City { get; set; }
        public int Pincode { get; set; }
    }

    public class InitChannel
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public string CommercialType { get; set; }
        public List<DeviceInfo> DeviceInfo { get; set; }
    }

    public class DeviceInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string InputType { get; set; }
        public string Regex { get; set; }
    }

    public class PaymentMode
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public List<PaymentInfo> PaymentInfo { get; set; }
    }

    public class PaymentInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string InputType { get; set; }
        public string Regex { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string InputType { get; set; }
        public int Mandatory { get; set; }
        public string Regex { get; set; }
    }

    public class PayOutRequest
    {
        public List<string> PaymentMethods { get; set; }
    }



}
