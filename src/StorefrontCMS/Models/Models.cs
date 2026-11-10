using System.ComponentModel.DataAnnotations;
namespace StorefrontCMS.Models;
public sealed class Product {
 public int Id {get;set;}
 [Range(1,int.MaxValue)] public int CategoryId {get;set;}
 [Required,StringLength(120)] public string Name {get;set;}="";
 [Required,StringLength(2000)] public string Description {get;set;}="";
 [Range(typeof(decimal),"0.01","999999.99")] public decimal Price {get;set;}
 [Range(0,1000000)] public int Stock {get;set;}
 public bool Active {get;set;}=true;
 public long Version {get;set;}
}
public sealed record Category(int Id,string Name);
public sealed record Catalog(IReadOnlyList<Product> Products,IReadOnlyList<Category> Categories,int Total,int Page,string? Q,int? CategoryId);
public sealed record CartLine(int ProductId,int Quantity);
public sealed record CartItem(Product Product,int Quantity) { public decimal Total=>Product.Price*Quantity; }
public sealed record OrderResult(int Code,int? OrderId);
public sealed record OrderSummary(int Id,DateTime CreatedUtc,decimal Total);
public sealed record OrderLine(string Name,int Quantity,decimal UnitPrice);
public sealed record OrderDetail(OrderSummary Order,IReadOnlyList<OrderLine> Lines);
public sealed class CmsEdit {
 [Required,RegularExpression("^[a-z][a-z0-9-]{0,39}$")] public string Slug {get;set;}="";
 [Required,StringLength(120)] public string Title {get;set;}="";
 [Required,StringLength(4000)] public string Body {get;set;}="";
}
public sealed record CmsVersion(int Id,string Slug,string Title,string Body,DateTime CreatedUtc);
public sealed record CmsBlock(string Slug,int? PublishedVersionId);
public sealed record CmsScreen(IReadOnlyList<CmsBlock> Blocks,IReadOnlyList<CmsVersion> Versions);
public sealed class LoginInput {
 [Required,StringLength(80)] public string Username {get;set;}="";
 [Required,StringLength(200),DataType(DataType.Password)] public string Password {get;set;}="";
}
public sealed record StoreUser(int Id,string Username,string PasswordHash,string Role);
public sealed class CheckoutInput { public Guid RequestId {get;set;} [Range(typeof(bool),"true","true",ErrorMessage="Confirm the mock payment.")] public bool Confirm {get;set;} }
