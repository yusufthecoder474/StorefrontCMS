using Microsoft.Extensions.Logging.Abstractions;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
using StorefrontCMS.Services;
using Xunit;
namespace StorefrontCMS.Tests;
public sealed class OrderServiceTests {
 private sealed class FakeRepository : IOrderRepository {
  public int Calls; public int User; public Guid Request; public string Correlation=""; public IReadOnlyList<CartLine> Items=[];
  public OrderResult Result=new(0,42);
  public Task<OrderResult> Place(int userId,Guid requestId,IReadOnlyList<CartLine> items,string correlationId){Calls++;User=userId;Request=requestId;Items=items;Correlation=correlationId;return Task.FromResult(Result);}
 }
 [Fact] public async Task Valid_order_preserves_identity_items_and_correlation() {
 var repo=new FakeRepository();var service=new OrderService(repo,NullLogger<OrderService>.Instance);var key=Guid.NewGuid();
 var result=await service.Place(7,key,[new(3,2)],"trace-123");
 Assert.Equal(new OrderResult(0,42),result);Assert.Equal(1,repo.Calls);Assert.Equal(7,repo.User);Assert.Equal(key,repo.Request);Assert.Equal("trace-123",repo.Correlation);Assert.Equal(new CartLine(3,2),Assert.Single(repo.Items));
 }
 [Theory][InlineData(0)][InlineData(-1)][InlineData(100)] public async Task Invalid_quantity_never_reaches_database(int quantity) {
 var repo=new FakeRepository();var service=new OrderService(repo,NullLogger<OrderService>.Instance);
 Assert.Equal(2,(await service.Place(1,Guid.NewGuid(),[new(1,quantity)],"x")).Code);Assert.Equal(0,repo.Calls);
 }
 [Fact] public async Task Empty_cart_is_rejected(){var repo=new FakeRepository();Assert.Equal(2,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(1,Guid.NewGuid(),[],"x")).Code);Assert.Equal(0,repo.Calls);}
 [Fact] public async Task Duplicate_products_are_rejected(){var repo=new FakeRepository();Assert.Equal(2,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(1,Guid.NewGuid(),[new(1,1),new(1,2)],"x")).Code);Assert.Equal(0,repo.Calls);}
 [Fact] public async Task Too_many_lines_are_rejected(){var repo=new FakeRepository();Assert.Equal(2,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(1,Guid.NewGuid(),Enumerable.Range(1,51).Select(i=>new CartLine(i,1)).ToList(),"x")).Code);Assert.Equal(0,repo.Calls);}
 [Theory][InlineData(1)][InlineData(2)] public async Task Business_codes_are_preserved(int code){var repo=new FakeRepository {Result=new(code,null)};Assert.Equal(code,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(1,Guid.NewGuid(),[new(1,1)],"x")).Code);Assert.Equal(1,repo.Calls);}
 [Fact] public async Task Missing_request_id_is_rejected(){var repo=new FakeRepository();Assert.Equal(2,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(1,Guid.Empty,[new(1,1)],"x")).Code);Assert.Equal(0,repo.Calls);}
 [Fact] public async Task Invalid_user_is_rejected(){var repo=new FakeRepository();Assert.Equal(2,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(0,Guid.NewGuid(),[new(1,1)],"x")).Code);Assert.Equal(0,repo.Calls);}
 [Fact] public async Task Invalid_product_is_rejected(){var repo=new FakeRepository();Assert.Equal(2,(await new OrderService(repo,NullLogger<OrderService>.Instance).Place(1,Guid.NewGuid(),[new(0,1)],"x")).Code);Assert.Equal(0,repo.Calls);}
}
