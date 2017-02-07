using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using AirShow.Models.ViewModels;
using AirShow.Models.Interfaces;
using AirShow.Models.Contexts;
using AirShow.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    public class AccountController : Controller
    {
        private SignInManager<User> _signInManager;
        private UserManager<User> _userManager;
        private IMailService _mailService;
        private AirShowContext _context;
        private IConfigurationRoot _configuration;
        private IHostingEnvironment _hostingEnvironment;

        public AccountController(UserManager<User> userManager,
                                 SignInManager<User> signInManager,
                                 AirShowContext context,
                                 IMailService mailService,
                                IConfigurationRoot configuration, 
                                IHostingEnvironment hostingEnvironment)
        {
            _mailService = mailService;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
        }

        // GET: /<controller>/
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userList = _userManager.Users.Where(u => u.UserName == model.Email).ToList();
                if (userList.Count() != 1)
                {
                    ModelState.AddModelError("", "No such email");
                    return View(model);
                }
                var user = userList.First();
                if (!user.EmailConfirmed)
                {
                    ModelState.AddModelError("", "Please confirm your email");
                    return View(model);
                }
                var loginResult = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (loginResult.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Email or password incorrect");
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var activationToken = GenerateActivationToken();
                var user = new User { UserName = model.Email, Name = model.Name, ActivationToken = activationToken,
                CreationDate = DateTime.Now};

                var created = await _userManager.CreateAsync(user, model.Password);
                if (created.Succeeded)
                {
                    var userId = user.Id;
                    var message = GenerateMessageForActivationToken(activationToken, userId);
                    var mailStatus = await _mailService.SendMessageToAddress(message, model.Email);
                    if (mailStatus.ErrorMessageIfAny != null)
                    {
                        ModelState.AddModelError("", "Could not send confirmation mail. Please try again later");
                    } else
                    {
                        // this is not very ok, but it will suffice for now
                        ModelState.AddModelError("", "Registration succeeded. Please check your e-mail to activate your account");
                    }
                }
                else
                {
                    foreach (var error in created.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View();
        }

        public async Task<IActionResult> ConfirmAccount(string userId, string activationToken)
        {
            var vm = new ActivationViewModel();
            var usersList = _userManager.Users.Where(u => u.Id == userId && u.ActivationToken == activationToken).ToList();
            if (usersList.Count() != 1)
            {
                vm.Message = "No account found. It is possible that the activation period expired.";
            } else
            {
                var user = usersList.First();
                if (user.EmailConfirmed)
                {
                    vm.Message = "Account already activated";
                } else
                {
                    user.EmailConfirmed = true;
                    var result =  await _userManager.UpdateAsync(user);
                     
                    if (!result.Succeeded)
                    {
                        vm.Message = "An error ocurred. Please try again later or make a new account";
                    } else
                    {
                        await _signInManager.SignInAsync(user, true);
                        vm.Message = "Account activated. You can upload presentations now";
                        vm.MessageHref = $"/{nameof(HomeController).WithoutControllerPart()}/"+
                            $"{nameof(HomeController.UploadPresentation)}";
                    }
                }
            }

            return View(vm);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }


        private string GenerateActivationToken()
        {
            var guid = Guid.NewGuid().ToString("N");
            while(_userManager.Users.Any(u => u.ActivationToken == guid))
            {
                guid = Guid.NewGuid().ToString("N");
            }

            return guid;
        }


        private string GenerateMessageForActivationToken(string activationToken, string userId)
        {
            var host = "";
            if (_hostingEnvironment.IsDevelopment())
            {
                host = _configuration["Hosts:development"];
            }
            else
            {
                host = _configuration["Hosts:production"];
            }

            var href = $"{host}/{nameof(AccountController).WithoutControllerPart()}/ConfirmAccount?" +
                $"userId={userId}&activationToken={activationToken}";

            var anchorPart = $"<a href=\"{href}\">In order to activate your account within AirShow, please click this link</a>";
            var orPart = $"<p>Or copy and paste the following url into your browser's navigation bar: <br>{href}</p>";
            var html = $"<html><head/><body>{anchorPart}\n{orPart}</body>";
            return html;
        }
    }
}
