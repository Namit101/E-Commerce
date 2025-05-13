using Ecom.Business.Services;
using Ecom.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace Ecom.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IProductService _productService; // Inject Product Service

        // test comment
        public UserController(IUserService userService, SignInManager<User> signInManager, UserManager<User> userManager, IProductService productService)
        {
            _userService = userService;
            _signInManager = signInManager;
            _userManager = userManager;
            _productService = productService;
        }

        // GET: /User/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _userService.RegisterUserAsync(model); // Role is not passed anymore

            if (result.Succeeded)
            {
                return RedirectToAction("Login");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // GET: /User/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /User/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    if (user != null)
                    {
                        if (user.Role == "ADMIN")
                            return RedirectToAction("AdminDashboard");
                        else if (user.Role == "CUSTOMER")
                            return RedirectToAction("CustomerDashboard");
                    }
                    ModelState.AddModelError("", "Failed to retrieve user information after login.");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                }
            }
            return View(model);
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AdminDashboard()
        {
            var customers = await _userService.GetCustomersAsync();
            return View("AdminDashboard", customers); // Pass the list of customers
        }

        [Authorize(Roles = "CUSTOMER")]
        public async Task<IActionResult> CustomerDashboard()
        {
            var products = await _productService.GetProductsAsync();
            return View("Dashboard", products); // Assuming you want to use the existing Dashboard view for customers and pass products
        }

        [Authorize]
        public async Task<IActionResult> Profile(string userId)
        {
            var user = await _userService.GetUserProfileAsync(userId);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User updatedUser)
        {
            var result = await _userService.UpdateUserProfileAsync(updatedUser);
            if (result.Succeeded)
            {
                TempData["Message"] = "Profile updated successfully.";
                return RedirectToAction("Profile", new { userId = updatedUser.Id });
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View("Profile", updatedUser);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}