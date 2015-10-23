using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using TestEmpty.ViewModels.Account;
using Microsoft.AspNet.Identity;
using ObjectStore.Identity;
//using ObjectStore;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TestEmpty.Controllers
{
    public class AccountController : Controller
    {
        readonly SignInManager<User> _signInManager;
        //readonly ObjectStore.Interfaces.IObjectProvider _objectProvider;
        //readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(SignInManager<User> signInManager) //, ObjectStore.Interfaces.IObjectProvider objectProvider, IPasswordHasher<User> passwordHasher)
        {
            _signInManager = signInManager;
            //_objectProvider = objectProvider;
            //_passwordHasher = passwordHasher;
        }

        // GET: /<controller>/
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            SignInResult result = await _signInManager.PasswordSignInAsync(model.Name, model.Password, false, false);

            if (result.Succeeded)
                return Redirect(returnUrl);

            //if (model.Name == "User1" && model.Password == "test")
            //{
            //    User newUser = _objectProvider.CreateObject<User>();
            //    newUser.Name = "User1";
            //    newUser.Password = _passwordHasher.HashPassword(newUser, "test");
            //    _objectProvider.GetQueryable<User>().Save();
            //}

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }
    }
}
