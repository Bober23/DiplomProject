using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using DiplomProject.Backend.ImageProcessingService.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var accessKey = Environment.GetEnvironmentVariable("S3_Access_key");
var secretKey = Environment.GetEnvironmentVariable("S3_Secret_key");
builder.Services.AddSingleton<IAmazonS3>(provider =>
{

    var config = new AmazonS3Config
    {
        SignatureVersion = "2",
        ServiceURL = "https://hb.ru-msk.vkcloud-storage.ru", // Ёндпоинт VK Cloud
        ForcePathStyle = true, // ќб€зательно дл€ VK Cloud,
    };

    if (accessKey != null && secretKey != null)
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        return new AmazonS3Client(credentials, config);
    }
    return new AmazonS3Client(config);

    
});
builder.Services.AddSingleton<IImageProcessor, ImageProcessor>();

var MyAllowSpecificOrigins = "AllowAll";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {// add the allowed origins 
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
