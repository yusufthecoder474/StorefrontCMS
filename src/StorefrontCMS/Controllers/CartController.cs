using Microsoft.AspNetCore.Mvc;
using StorefrontCMS.Services;
namespace StorefrontCMS.Controllers;
public sealed class CartController(CartService cart):Controller {
 public async Task<IActionResult> Index()=>View(await cart.Items());
 [HttpGet] public async Task<IActionResult> Summary()=>Json(await SummaryData());
 private async Task<object> SummaryData() {var items=await cart.Items();return new {count=items.Sum(x=>x.Quantity),total=items.Sum(x=>x.Total),items=items.Select(x=>new {id=x.Product.Id,name=x.Product.Name,quantity=x.Quantity,total=x.Total})};}
 [HttpPost] public async Task<IActionResult> Set(int productId,int quantity,bool add=false) {
 if(!ModelState.IsValid) return BadRequest(new {error="Invalid cart input."});
 try {await cart.Set(productId,quantity,add);return Json(await SummaryData());}catch(ArgumentException ex){return BadRequest(new {error=ex.Message});}
 }
}
