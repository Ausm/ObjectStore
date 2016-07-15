using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TestEmpty.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using ObjectStore.Identity;
using Microsoft.AspNetCore.Authorization;
#if DEBUG
using ObjectStore;
#endif

namespace TestEmpty.Controllers
{
    public class AccountController : Controller
    {
        readonly SignInManager<User> _signInManager;

#if !DEBUG
        public AccountController(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }
#else
        readonly ObjectStore.Interfaces.IObjectProvider _objectProvider;
        readonly IPasswordHasher<User> _passwordHasher;
        public AccountController(SignInManager<User> signInManager, ObjectStore.Interfaces.IObjectProvider objectProvider, IPasswordHasher<User> passwordHasher)
        {
            _signInManager = signInManager;
            _objectProvider = objectProvider;
            _passwordHasher = passwordHasher;
        }
#endif

        // GET: /<controller>/
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(model.Name, model.Password, false, false);

            if (result.Succeeded)
                return Redirect(returnUrl);

            ViewBag.ReturnUrl = returnUrl;
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

#if DEBUG
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            User newUser = _objectProvider.CreateObject<User>();
            newUser.Name = "User";
            newUser.Password = _passwordHasher.HashPassword(newUser, "test");
            _objectProvider.GetQueryable<User>().Save();

            return View("Login");
        }
#endif
    }
}
