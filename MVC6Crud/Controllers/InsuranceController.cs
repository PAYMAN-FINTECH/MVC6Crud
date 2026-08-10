using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC6Crud.Data;
using MVC6Crud.Models;
using Org.BouncyCastle.Crypto.Generators;

namespace MVC6Crud.Controllers
{
    public class InsuranceController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly DataUtils _dataUtils;

        public InsuranceController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration, DataUtils dataUtils)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _dataUtils = dataUtils;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Services()
        {
            return View();
        }
        public IActionResult RefundPolcy()
        { 
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        // 🔹 LOGIN
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] InLoginRequest request)
        {
            var user = await _context.errorModels
                .FirstOrDefaultAsync(x => x.respTime == request.Password && (x.agId == request.Mobile || x.reqTime == request.Mobile));

            if (user == null)
            {
                return Ok(new ApiResponseIn
                {
                    IsSuccess = false,
                    Message = "Invalid mobile or password"
                });
            }

            HttpContext.Session.SetString("UserName", user.payload);  // Store user name in session
            HttpContext.Session.SetString("UserPhone", user.agId); // Optionally store phone
            HttpContext.Session.SetString("Email", user.reqTime);


            return Ok(new ApiResponseIn
            {
                IsSuccess = true,
                Message = "Login successful",
                Data = new
                {
                    user = new { user.payload, user.agId }
                }
            });
        }

        //// 🔹 REGISTER
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] InRegisterRequest request)
        {
            if (await _context.errorModels.AnyAsync(x => x.agId == request.Mobile || x.reqTime == request.Email))
            {
                return Ok(new ApiResponseIn
                {
                    IsSuccess = false,
                    Message = "Mobile number already registered"
                });
            }

            var user = new ErrorModel
            {
                payload = request.Name,
                agId = request.Mobile,
                reqTime = request.Email,
                respTime = request.Password,
                requestId = "",
                uid = "",
                statuscode = true,
                jsonBody = ""
            };

            _context.errorModels.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponseIn
            {
                IsSuccess = true,
                Message = "User registered successfully"
            });
        }
        //// 🔹 FORGOT PASSWORD (OTP stub)
        //[HttpPost("forgot-password")]
        //public async Task<IActionResult> ForgotPassword(InForgotPasswordRequest request)
        //{
        //    var user = await _context.Users
        //        .FirstOrDefaultAsync(x => x.Mobile == request.Mobile);

        //    if (user == null)
        //    {
        //        return Ok(new ApiResponse
        //        {
        //            IsSuccess = false,
        //            Message = "Mobile number not found"
        //        });
        //    }

        //    // TODO: Integrate SMS OTP
        //    return Ok(new ApiResponse
        //    {
        //        IsSuccess = true,
        //        Message = "OTP sent to registered mobile number"
        //    });
        //}
    }

    public class ApiResponseIn
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class InLoginRequest
    {
        public string Mobile { get; set; }
        public string Password { get; set; }
    }

    public class InRegisterRequest
    {
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class InForgotPasswordRequest
    {
        public string Mobile { get; set; }
    }
}
