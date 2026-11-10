using System.Text.Json;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
namespace StorefrontCMS.Services;
public sealed class CartService(IHttpContextAccessor accessor,StoreRepository repository) {
 private ISession Session=>accessor.HttpContext!.Session;
 public List<CartLine> Read()=>JsonSerializer.Deserialize<List<CartLine>>(Session.GetString("cart")??"[]")!;
 public void Clear()=>Session.Remove("cart");
 public async Task Set(int productId,int quantity,bool add=false) {
 var lines=Read(); var existing=lines.SingleOrDefault(x=>x.ProductId==productId);
 if(add) quantity+=existing?.Quantity??0;
 if(quantity<0 || quantity>99 || productId<=0) throw new ArgumentException("Quantity must be between 0 and 99.");
 if(quantity>0) {
 var product=await repository.Product(productId)??throw new ArgumentException("Product is no longer available.");
 if(product.Stock<quantity) throw new ArgumentException("Not enough stock for this quantity.");
 if(existing is null && lines.Count>=50) throw new ArgumentException("Cart supports at most 50 products.");
 }
 lines.RemoveAll(x=>x.ProductId==productId); if(quantity>0) lines.Add(new(productId,quantity));
 Session.SetString("cart",JsonSerializer.Serialize(lines));
 }
 public async Task<List<CartItem>> Items() {
 var result=new List<CartItem>();
 foreach(var line in Read()) { var p=await repository.Product(line.ProductId); if(p is not null) result.Add(new(p,line.Quantity)); else result.Add(new(new Product {Id=line.ProductId,Name="Unavailable product — remove before checkout",Price=0,Stock=0},line.Quantity)); }
 return result;
 }
}
