using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using StorefrontCMS.Models;
namespace StorefrontCMS.Data;
public interface IOrderRepository { Task<OrderResult> Place(int userId,Guid requestId,IReadOnlyList<CartLine> items,string correlationId); }
public sealed class StoreRepository(IConfiguration config):IOrderRepository {
 public SqlConnection Open()=>new(config.GetConnectionString("Store") ?? throw new InvalidOperationException("Missing Store connection string."));
 public async Task<List<T>> Query<T>(string procedure,object? args=null) { using var c=Open(); return (await c.QueryAsync<T>(procedure,args,commandType:CommandType.StoredProcedure)).AsList(); }
 public async Task Execute(string procedure,object args) { using var c=Open(); await c.ExecuteAsync(procedure,args,commandType:CommandType.StoredProcedure); }
 public async Task<Catalog> Search(string? q,int? categoryId,int page) {
 using var c=Open(); using var grid=await c.QueryMultipleAsync("dbo.ProductSearch",new {Q=q,CategoryId=categoryId,Page=page},commandType:CommandType.StoredProcedure);
 var products=(await grid.ReadAsync<Product>()).AsList(); var total=await grid.ReadSingleAsync<int>();
 return new(products,await Query<Category>("dbo.CategoryList"),total,page,q,categoryId);
 }
 public async Task<Product?> Product(int id)=>(await Query<Product>("dbo.ProductGet",new {Id=id})).SingleOrDefault();
 public async Task<OrderResult> Place(int userId,Guid requestId,IReadOnlyList<CartLine> items,string correlationId) {
 var table=new DataTable(); table.Columns.Add("ProductId",typeof(int)); table.Columns.Add("Quantity",typeof(int)); foreach(var line in items) table.Rows.Add(line.ProductId,line.Quantity);
 using var c=Open(); return await c.QuerySingleAsync<OrderResult>("dbo.PlaceOrder",new {UserId=userId,RequestId=requestId,Items=table.AsTableValuedParameter("dbo.CartTable"),CorrelationId=correlationId},commandType:CommandType.StoredProcedure);
 }
 public async Task<OrderDetail?> Detail(int userId,int id) {
 using var c=Open(); using var g=await c.QueryMultipleAsync("dbo.OrderDetail",new {UserId=userId,Id=id},commandType:CommandType.StoredProcedure);
 var order=await g.ReadSingleOrDefaultAsync<OrderSummary>(); var lines=(await g.ReadAsync<OrderLine>()).AsList(); return order is null?null:new(order,lines);
 }
 public async Task<CmsScreen> CmsList() {
 using var c=Open(); using var g=await c.QueryMultipleAsync("dbo.CmsList",commandType:CommandType.StoredProcedure);
 return new((await g.ReadAsync<CmsBlock>()).AsList(),(await g.ReadAsync<CmsVersion>()).AsList());
 }
}
