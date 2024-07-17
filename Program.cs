using EmployeeWindow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using EmployeeWindow.Models;
using EmployeeWindow.Hubs;
using OpenAI;
using EmployeeWindow.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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

builder.Services.AddSingleton<HistoryService>();

var openaiUri = new Uri(builder.Configuration["OpenAI:url"]+ "/chat/completions");


builder.Services.AddKernel();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddOpenAIChatCompletion(
    modelId: builder.Configuration["OpenAI:model"],
        endpoint: openaiUri,
        apiKey: builder.Configuration["OpenAI:ApiKey"]);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var cohereService = new CohereService(builder.Configuration["Cohere:ApiKey"], builder.Configuration["Cohere:url"],builder.Configuration["Cohere:RerankModel"]);
builder.Services.AddSingleton(cohereService);

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
