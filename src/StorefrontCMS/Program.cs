using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StorefrontCMS.Data;
using StorefrontCMS.Models;
using StorefrontCMS.Services;
var builder=WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); builder.Logging.AddJsonConsole(o=>o.IncludeScopes=true);
builder.Services.AddControllersWithViews(o=>o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o=>o.HeaderName="X-CSRF-TOKEN");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
 o.LoginPath="/Account/Login"; o.AccessDeniedPath="/Account/Denied"; o.Cookie.HttpOnly=true; o.Cookie.SameSite=SameSiteMode.Lax; o.ExpireTimeSpan=TimeSpan.FromHours(2);
});
builder.Services.AddAuthorization(); builder.Services.AddDistributedMemoryCache(); builder.Services.AddMemoryCache();
builder.Services.AddSession(o=>{o.Cookie.HttpOnly=true;o.Cookie.IsEssential=true;o.IdleTimeout=TimeSpan.FromHours(2);});
builder.Services.AddHttpContextAccessor();builder.Services.AddSingleton<CmsGate>();
builder.Services.AddScoped<StoreRepository>(); builder.Services.AddScoped<IOrderRepository>(s=>s.GetRequiredService<StoreRepository>());
builder.Services.AddScoped<OrderService>(); builder.Services.AddScoped<CartService>(); builder.Services.AddScoped<CmsService>();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddPolicy("login",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions {PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
var keys=builder.Configuration["DataProtectionPath"]; if(!string.IsNullOrEmpty(keys)) builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys));
var app=builder.Build();
app.Use(async(ctx,next)=>{
 var id=Guid.NewGuid().ToString("N");ctx.TraceIdentifier=id;ctx.Response.Headers["X-Correlation-ID"]=id;
 using var scope=app.Logger.BeginScope(new Dictionary<string,object>{{"CorrelationId",id}});
 var timer=System.Diagnostics.Stopwatch.StartNew();
 try {await next();} catch(Exception ex) {
 app.Logger.LogError(ex,"Unhandled request error {Path}",ctx.Request.Path);
 if(ctx.Response.HasStarted) throw;
 ctx.Response.Clear();ctx.Response.StatusCode=500;ctx.Response.Headers["X-Correlation-ID"]=id;
 if(ctx.Request.Headers["X-Requested-With"]=="XMLHttpRequest") await ctx.Response.WriteAsJsonAsync(new {error="Something went wrong. Please retry.",correlationId=id});
 else {ctx.Response.ContentType="text/html; charset=utf-8";await ctx.Response.WriteAsync($"<h1>Something went wrong</h1><p>Please retry. Support reference: {id}</p><a href='/'>Return to store</a>");}
 } finally {app.Logger.LogInformation("HTTP {Method} {Path} returned {Status} in {ElapsedMs} ms",ctx.Request.Method,ctx.Request.Path,ctx.Response.StatusCode,timer.ElapsedMilliseconds);}
});
app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter(); app.UseSession(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health",async(StoreRepository repository)=>{try {await repository.Query<int>("dbo.HealthCheck");return Results.Json(new {status="healthy"});}catch(Exception ex){app.Logger.LogError(ex,"Database health check failed");return Results.Json(new {status="unhealthy"},statusCode:503);}});
app.MapControllerRoute("default","{controller=Catalog}/{action=Index}/{id?}");
// Provision demo accounts once. Secrets come exclusively from environment/user secrets.
using(var scope=app.Services.CreateScope()) {
 var repo=scope.ServiceProvider.GetRequiredService<StoreRepository>();var hasher=new PasswordHasher<string>();
 foreach(var (name,role,key) in new[]{("customer","Customer","DemoCustomerPassword"),("admin","Admin","DemoAdminPassword")}) {
 var password=builder.Configuration[key]??throw new InvalidOperationException($"Set {key} before starting.");
 if(password.Length<12) throw new InvalidOperationException($"{key} must contain at least 12 characters.");
 await repo.Execute("dbo.UserBootstrap",new {Username=name,PasswordHash=hasher.HashPassword(name,password),Role=role});
 }
}
app.Run();
