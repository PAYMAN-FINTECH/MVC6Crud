using Microsoft.EntityFrameworkCore;
using MVC6Crud.Models;
using MVC6Crud.Models.App;
using MVC6Crud.Models.PaymanApp;

namespace MVC6Crud.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        //public DbSet<Category> Categories { get; set; }
        //public DbSet<Product> Products { get; set; }       

        public DbSet<User> users { get; set; }
        public DbSet<PaymentDetails> paymentDetails { get; set; }

        public DbSet<PaymentTransectionId> paymentTransectionIds { get; set; }
        public DbSet<PayOut> payOuts { get; set; }

        public DbSet<SafexPayOut> safexPayOuts { get; set; }
        public DbSet<BeneficiaryAccounts> beneficiaryAccounts { get; set; }
        public DbSet<AddBeneficiaryAccounts> addBeneficiaryAccounts { get; set; }
        public DbSet<PayOutTransectionDetails> payOutTransectionDetails { get; set; }
        public DbSet<ErrorModel> errorModels { get; set; }
        public DbSet<PayIn> payIns { get; set; }
        public DbSet<PayOutConfic> payOutConfics { get; set; }
        public DbSet<BankDetails> bankDetails { get; set; }
        public DbSet<AdharVerification> adharVerifications { get; set; }
        public DbSet<PayManGateWayMarigin> payManGateWayMarigins { get; set; }
        public DbSet<AccountVerificationBanks>  accountVerificationBanks { get; set; }
        public DbSet<EasebuzzTime> easebuzzTimes { get; set; }
        public DbSet<PayManUsers> payManUsers { get; set; }
        public DbSet<AadharDetails> aadharDetails { get; set; }
        public DbSet<UserDocuments> userDocuments { get; set; }
        public DbSet<PayManPayOut> payManPayOuts { get; set; }
        public DbSet<PayManBeneficiaryAccounts> payManBeneficiaryAccounts { get; set; }
        public DbSet<PayManPayIn> payManPayIns   { get; set; }
        public DbSet<PayManHistory> payManHistories { get; set; }
        public DbSet<PayManGateways> PayManGateways  { get; set; }
        public DbSet<BillAvenueCreditCardBillers> billAvenueCreditCardBillers { get; set; }
        public DbSet<PayInProfiles> payInProfiles { get; set; }
        
        public DbSet<SignedDocumentAgreement> SignedDocumentAgreements { get; set; }
        public DbSet<Gateways> gateways { get; set; }
        public DbSet<UserLookUp> userLookUps { get; set; }


        //new system models

        public DbSet<AppUser> AppUser { get; set; }
        public DbSet<OtpLog> OtpLogs { get; set; }

        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<PMUsers> pMUsers { get; set; }
        public DbSet<AadharKycDetails> aadharKycDetails { get; set; }
        public DbSet<PanDetails> panDetails { get; set; }
        public DbSet<AadharDocument> aadharDocuments { get; set; }
        public DbSet<PaymanCards> paymanCards { get; set; }
    }
}
