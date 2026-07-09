using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("connection") ?? throw new InvalidOperationException("Connection string 'Art_GalleryContextConnection' not found.");;

builder.Services.AddDbContext<Art_GalleryContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<Art_GalleryUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<Art_GalleryContext>();
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();
app.Run();
