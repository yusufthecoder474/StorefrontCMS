using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
using StorefrontCMS.Services;
namespace StorefrontCMS.Controllers;
[Authorize]
public sealed class OrdersController(StoreRepository repository,CartService cart,OrderService service):Controller {
 private int UserId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task<IActionResult> Index()=>View(await repository.Query<OrderSummary>("dbo.OrderHistory",new {UserId}));
 public async Task<IActionResult> Details(int id){var detail=await repository.Detail(UserId,id);return detail is null?NotFound():View(detail);}
 [HttpGet] public IActionResult Checkout() {var key=HttpContext.Session.GetString("checkout");if(key is null){key=Guid.NewGuid().ToString();HttpContext.Session.SetString("checkout",key);}return View(new CheckoutInput {RequestId=Guid.Parse(key)});}
 [HttpPost] public async Task<IActionResult> Checkout(CheckoutInput input) {
 if(!ModelState.IsValid)return View(input);
 if(input.RequestId==Guid.Empty || HttpContext.Session.GetString("checkout")!=input.RequestId.ToString()) return BadRequest("Checkout expired. Open checkout again.");
 var result=await service.Place(UserId,input.RequestId,cart.Read(),HttpContext.TraceIdentifier);
 if(result.Code!=0){ModelState.AddModelError("",result.Code==1?"Stock changed or a product is unavailable. Update your cart and retry.":"Your cart is empty or invalid.");return View(input);}
 cart.Clear();HttpContext.Session.Remove("checkout");return RedirectToAction(nameof(Details),new {id=result.OrderId});
 }
}
