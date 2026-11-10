using Microsoft.Extensions.Caching.Memory;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
namespace StorefrontCMS.Services;
// One gate prevents an in-flight reader from repopulating an old value after publish.
public sealed class CmsGate { public SemaphoreSlim Value {get;}=new(1,1); }
public sealed class CmsService(StoreRepository repository,IMemoryCache cache,CmsGate gate) {
 public async Task<CmsVersion?> Published(string slug) {
 await gate.Value.WaitAsync(); try {
 if(cache.TryGetValue<CmsVersion>("cms:"+slug,out var value)) return value;
 value=(await repository.Query<CmsVersion>("dbo.CmsPublished",new {Slug=slug})).SingleOrDefault();
 if(value is not null) cache.Set("cms:"+slug,value,TimeSpan.FromMinutes(2));
 return value;
 } finally {gate.Value.Release();}
 }
 public async Task Publish(string slug,int? versionId,int userId,string correlationId) {
 await gate.Value.WaitAsync(); try {
 if(versionId.HasValue) await repository.Execute("dbo.CmsPublish",new {Slug=slug,VersionId=versionId.Value,UserId=userId,CorrelationId=correlationId});
 else await repository.Execute("dbo.CmsRollback",new {Slug=slug,UserId=userId,CorrelationId=correlationId});
 cache.Remove("cms:"+slug);
 } finally {gate.Value.Release();}
 }
}
