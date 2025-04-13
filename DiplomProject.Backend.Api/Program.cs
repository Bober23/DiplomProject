using DiplomProject;
using DiplomProject.Backend.Api;
using DiplomProject.Backend.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
string? databaseConnectionString = "Host=37.252.22.178;Port=5432;Database=diplom;Username=bober;Password=bober"; //TODO: спрятать констринг
builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(databaseConnectionString));

builder.Services.Configure<ServicesOptions>(builder.Configuration.GetSection("Services"));
builder.Services.AddScoped<IUserModel, UserModel>();
builder.Services.AddScoped<IDocumentModel, DocumentModel>();
builder.Services.AddSingleton<HttpClient>();
var MyAllowSpecificOrigins = "AllowAll";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000"); // add the allowed origins  
                          policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();



                      });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();

app.MapControllers();

app.Run();
