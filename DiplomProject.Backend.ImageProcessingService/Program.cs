using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using DiplomProject.Backend.ImageProcessingService.Model;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = "https://hb.vkcs.cloud", // Укажите эндпоинт
        ForcePathStyle = true,
        RegionEndpoint = RegionEndpoint.GetBySystemName("ru-msk"),
        SignatureVersion = "4", // Используйте SigV4
        UseAccelerateEndpoint = false,
        
    };

    return new AmazonS3Client(config);
});
builder.Services.AddSingleton<IImageProcessor, ImageProcessor>();
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
