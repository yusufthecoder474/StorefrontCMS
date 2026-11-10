using StorefrontCMS.Data;
using StorefrontCMS.Models;
namespace StorefrontCMS.Services;
public sealed class OrderService(IOrderRepository repository,ILogger<OrderService> logger) {
 public async Task<OrderResult> Place(int userId,Guid requestId,IReadOnlyList<CartLine> items,string correlationId) {
 if(userId<=0 || requestId==Guid.Empty || items.Count is 0 or >50 || items.Any(x=>x.ProductId<=0 || x.Quantity is <1 or >99) || items.Select(x=>x.ProductId).Distinct().Count()!=items.Count) return new(2,null);
 var result=await repository.Place(userId,requestId,items,correlationId);
 logger.LogInformation("Checkout completed with Code {Code}, OrderId {OrderId}, UserId {UserId}",result.Code,result.OrderId,userId);
 return result;
 }
}
