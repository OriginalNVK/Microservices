using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text;
using ProductService.AutoMapper;
using ProductService.Data;
using ProductService.Kafka;
using ProductService.Repositories;
using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NVKShop - Product Service API",
        Version = "v1",
        Description = "Manage products and categories"
    });
});

builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service v1"));

app.UseCors();
app.MapControllers();

app.Run();
