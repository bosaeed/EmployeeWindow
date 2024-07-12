using EmployeeWindow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using EmployeeWindow.Models;
using EmployeeWindow.Hubs;
using OpenAI;
using EmployeeWindow.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultDbConnection")));

builder.Services.AddDefaultIdentity<User>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
   
}).AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<MyDbContext>();


builder.Services.AddSession();
builder.Services.AddHttpClient();


builder.Services.AddSignalR();

OpenAIClientOptions openaiOptions = new OpenAIClientOptions() { 
Endpoint = new Uri( builder.Configuration["OpenAI:url"])
};
builder.Services.AddSingleton<OpenAIClient>(_ => new OpenAIClient(builder.Configuration["OpenAI:ApiKey"], openaiOptions));

builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");


app.MapRazorPages();

app.MapHub<ChatHub>("/chatHub");

await User.SeedAdminUser(app.Services);
app.Run();
