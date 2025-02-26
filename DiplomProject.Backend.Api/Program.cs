using DiplomProject;
using DiplomProject.Backend.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
string? databaseConnectionString = "Host=89.23.117.56;Port=5432;Database=Diplom;Username=Boberman;Password=bober23"; //TODO: спрятать констринг
builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(databaseConnectionString));

builder.Services.AddScoped<IUserModel, UserModel>();
builder.Services.AddScoped<IDocumentModel, DocumentModel>();
builder.Services.AddSingleton<HttpClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
