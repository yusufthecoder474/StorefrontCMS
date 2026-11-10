using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
using StorefrontCMS.Services;
namespace StorefrontCMS.Controllers;
[Authorize(Roles="Admin")]
public sealed class AdminController(StoreRepository repository,CmsService cms):Controller {
 public async Task<IActionResult> Products(int page=1) {if(page<1||page>100000)return BadRequest();return View(await repository.Search(null,null,page));}
 [HttpGet] public async Task<IActionResult> Edit(int id=0){ViewBag.Categories=await repository.Query<Category>("dbo.CategoryList");var p=id==0?new Product():await repository.Product(id);return p is null?NotFound():View(p);}
 [HttpPost] public async Task<IActionResult> Edit(Product p) {
 var categories=await repository.Query<Category>("dbo.CategoryList");ViewBag.Categories=categories;
 if(!categories.Any(x=>x.Id==p.CategoryId))ModelState.AddModelError("CategoryId","Choose an existing category.");
 if(!ModelState.IsValid)return View(p);
 try{await repository.Execute("dbo.ProductSave",new {p.Id,p.CategoryId,p.Name,p.Description,p.Price,p.Stock,p.Version});}
 catch(SqlException ex) when(ex.Number==51003){ModelState.AddModelError("",ex.Message);return View(p);}
 return RedirectToAction(nameof(Products));
 }
 [HttpPost] public async Task<IActionResult> Delete(int id,long version){try{await repository.Execute("dbo.ProductDelete",new {Id=id,Version=version});}catch(SqlException ex) when(ex.Number==51003){TempData["Message"]=ex.Message;}return RedirectToAction(nameof(Products));}
 public async Task<IActionResult> Cms()=>View(await repository.CmsList());
 [HttpGet] public async Task<IActionResult> Draft(int? id) {
 var v=id.HasValue?(await repository.CmsList()).Versions.SingleOrDefault(x=>x.Id==id):null;
 if(id.HasValue&&v is null)return NotFound();return View(v is null?new CmsEdit():new CmsEdit {Slug=v.Slug,Title=v.Title,Body=v.Body});
 }
 [HttpPost] public async Task<IActionResult> Draft(CmsEdit input){if(!ModelState.IsValid)return View(input);await repository.Execute("dbo.CmsDraft",new {input.Slug,input.Title,input.Body});return RedirectToAction(nameof(Cms));}
 [HttpPost] public async Task<IActionResult> Publish(string slug,int? versionId){
 if(!ModelState.IsValid || string.IsNullOrEmpty(slug) || slug.Length>40)return BadRequest();
 try{await cms.Publish(slug,versionId,int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),HttpContext.TraceIdentifier);TempData["Message"]="Publication updated.";}
 catch(SqlException ex) when(ex.Number is 51004 or 51005){TempData["Message"]=ex.Message;}
 return RedirectToAction(nameof(Cms));
 }
}
