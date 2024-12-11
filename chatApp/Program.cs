using chatApp.DB;
using chatApp.Hubs;
using chatApp.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Add services to the container.
builder.Services.AddDbContextPool<ChatDbContext>(opt =>
    opt.UseInMemoryDatabase("MyDB")
    //opt.UseNpgsql(builder.Configuration.GetConnectionString("BloggingContext"))
    );
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));
builder.Services.AddScoped<IRegisterRepository, RegisterRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUnreadStatusRepository, UnreadStatusRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddAutoMapper(typeof(ChatService));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:3000","http://localhost:3001").AllowAnyHeader().AllowAnyMethod().AllowCredentials());
    
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors();
app.UseAuthorization();

app.MapRazorPages();

app.MapHub<ChatHub>("/chatHub");

// Seed Database

AppDbInitializer.Seed(app);

app.Run();
