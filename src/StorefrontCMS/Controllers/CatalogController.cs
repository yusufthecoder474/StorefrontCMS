using Microsoft.AspNetCore.Mvc;
using StorefrontCMS.Data;
using StorefrontCMS.Services;
namespace StorefrontCMS.Controllers;
public sealed class CatalogController(StoreRepository repository,CmsService cms):Controller {
 public async Task<IActionResult> Index(string? q,int? categoryId,int page=1) {
 if(page<1 || page>100000 || q?.Length>120) return BadRequest();
 ViewBag.Banner=await cms.Published("homepage-banner");ViewBag.Promo=await cms.Published("promo-strip");
 return View(await repository.Search(string.IsNullOrWhiteSpace(q)?null:q.Trim(),categoryId,page));
 }
 public async Task<IActionResult> Details(int id) {var p=await repository.Product(id);return p is null?NotFound():View(p);}
 public async Task<IActionResult> Page(string slug="about") {var block=await cms.Published(slug);return block is null?NotFound():View(block);}
}
