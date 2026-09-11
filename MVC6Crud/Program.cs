
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MVC6Crud.Controllers;
using MVC6Crud.Data;
using MVC6Crud.Models.Airpay;
using MVC6Crud.Models.SabPaisa;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MVC6CrudConnetionString"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(60);

            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);


// Configure JWT authentication
var key = Encoding.UTF8.GetBytes("ThisIsA32ByteLongSecretKeyForJWT"); // Change to a secure key

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Change to true if you have an issuer
            ValidateAudience = false, // Change to true if you have an audience
            ValidateLifetime = true
        };
    });



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.ExpireTimeSpan = TimeSpan.FromMinutes(60 * 1);
        option.LoginPath = "/LogIn/LogIn";
        option.AccessDeniedPath = "/LogIn/LogIn";
    });

builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromMinutes(10);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});
// Register DataUtils + HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DataUtils>();
builder.Services.AddScoped<BBPSService>();
builder.Services.AddScoped<SabPaisaService>();
//builder.Services.AddHttpClient<DigiLockerService>();
builder.Services.AddScoped<DigiLockerService>();
//builder.Services.Configure<CFConfig>(Configuration.GetSection("Cashfree"));
builder.Services.AddHttpClient<CashfreeService>();
builder.Services.AddScoped<OperationService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<JiopayService>();
builder.Services.AddScoped<PaytmService>();
builder.Services.AddHttpClient();
builder.Services.AddTransient<AgreementPdf>();

builder.Services.Configure<AirpaySettings>(
    builder.Configuration.GetSection("Airpay"));

builder.Services.Configure<SabPaisaOptions>(
    builder.Configuration.GetSection("SabPaisa"));

builder.Services.AddSingleton<
    AirpayCryptoService>();

builder.Services.AddSingleton<
    AirpayCrc32>();

builder.Services.AddHttpClient(
    "Airpay",
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(60);
    });

builder.Services.AddScoped<
    AirpayService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.Environment.IsProduction();
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();

app.UseAuthorization();
app.UseCors("AllowAll");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
