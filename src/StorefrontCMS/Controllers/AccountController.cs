using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
namespace StorefrontCMS.Controllers;
public sealed class AccountController(StoreRepository repository):Controller {
 [HttpGet] public IActionResult Login()=>View(new LoginInput());
 [HttpPost,EnableRateLimiting("login")] public async Task<IActionResult> Login(LoginInput input) {
 if(!ModelState.IsValid)return View(input);
 var user=(await repository.Query<StoreUser>("dbo.UserGet",new {input.Username})).SingleOrDefault();
 if(user is null || new PasswordHasher<string>().VerifyHashedPassword(user.Username,user.PasswordHash,input.Password)==PasswordVerificationResult.Failed) {ModelState.AddModelError("","Invalid username or password.");return View(input);}
 var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.Username),new Claim(ClaimTypes.Role,user.Role)},CookieAuthenticationDefaults.AuthenticationScheme);
 HttpContext.Session.Clear();
 await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity)); return RedirectToAction("Index","Catalog");
 }
 [HttpPost] public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();HttpContext.Session.Clear();return RedirectToAction("Index","Catalog");}
 public IActionResult Denied()=>StatusCode(403,"Administrator access is required.");
}
