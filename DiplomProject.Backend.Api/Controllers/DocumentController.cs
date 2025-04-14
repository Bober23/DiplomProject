using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using iText.Layout.Element;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace DiplomProject.Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentModel _model;
        private readonly HttpClient _http;
        private readonly ServicesOptions _servicesOptions;
        public DocumentController(IDocumentModel model, HttpClient client, IOptions<ServicesOptions> options)
        {
            _model = model;
            _http = client;
            _servicesOptions = options.Value;
        }

        [EnableCors("AllowAll")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDocumentById(int id)
        {
            var response = await _model.GetDocumentById(id);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpGet("user/{id:int}")]
        public async Task<IActionResult> GetDocumentByUserId(int id)
        {
            var response = await _model.GetDocumentByUserId(id);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpPost]
        public async Task<IActionResult> CreateNewDocument(DocumentParameterRequest request)
        {
            var response = await _model.CreateNewDocument(request);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var response = await _model.DeleteDocument(id);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpDelete("{id:int}/{idImage:int}")]
        public async Task<IActionResult> DeleteDocumentImage(int id, int idImage)
        {
            var response = await _model.DeleteDocumentImage(id, idImage);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpPatch("namecat/{id:int}")]
        public async Task<IActionResult> UpdateDocumentName(int id, [FromBody] DocumentRenameRequest request)
        {
            var response = await _model.UpdateDocumentName(id, request.Name);
            var response2 = await _model.UpdateDocumentCategory(id, request.Category);
            if (response.HttpStatus == 200 && response2.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> UpdateDocumentCategory(int id, [FromQuery] string category)
        {
            var response = await _model.UpdateDocumentCategory(id, category);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [EnableCors("AllowAll")]
        [HttpPatch("image/{id:int}")]
        public async Task<IActionResult> AddImageToDocument(int id, IFormFile file)
        {
            var parentDocumentResponse = await _model.GetDocumentById(id);
            if (parentDocumentResponse.HttpStatus != 200 || parentDocumentResponse.Value == null)
            {
                return BadRequest($"Error {parentDocumentResponse.HttpStatus} occurs while search parent document: {parentDocumentResponse.Message}");
            }
            var parentDocument = parentDocumentResponse.Value;

            using var formData = new MultipartFormDataContent();

            // Читаем файл в поток
            using var fileStream = file.OpenReadStream();

            // Создаем StreamContent для файла
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            string fileName = file.FileName;
            if (parentDocument.ImageFiles.FirstOrDefault(x=>x.Name == fileName) != null)
            {
                fileName = fileName + DateTime.Now;
            }
            // Добавляем файл в форму
            formData.Add(fileContent, "file", file.FileName);

            // Отправляем POST-запрос
            var response = await _http.PostAsync($"{_servicesOptions.ImageService}/Image?fileDirectory={parentDocument.User.Id}/{parentDocument.id}/images", formData);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(response.Headers.ToString());
            }
            var linkToFile = await response.Content.ReadAsStringAsync();
            DocFile docFile = new DocFile()
            {
                Document = parentDocument,
                LinkToFile = linkToFile,
                LoadDate = DateTime.Now,
                Name = file.FileName,
            };
            var responseFromModel = await _model.AddDocFileToDocument(docFile);
            return Ok(responseFromModel.Value);
        }

        [EnableCors("AllowAll")]
        [HttpGet("images/{id:int}")]
        public async Task<IActionResult> GetDocumentImages(int id, [FromQuery] bool binarized)
        {
            var parentDocumentResponse = await _model.GetDocumentById(id);
            if (parentDocumentResponse.HttpStatus != 200 || parentDocumentResponse.Value == null)
            {
                return BadRequest($"Error {parentDocumentResponse.HttpStatus} occurs while search parent document: {parentDocumentResponse.Message}");
            }
            var parentDocument = parentDocumentResponse.Value;
            var archiveStream = new MemoryStream();
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {
                foreach (var image in parentDocument.ImageFiles)
                {
                    HttpResponseMessage? response = new HttpResponseMessage();
                    
                    response = await _http.GetAsync($"{_servicesOptions.ImageService}/Image?linkToFile={image.LinkToFile}&binarized={binarized}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest("Cannot Load Images");
                    }
                    var entry = archive.CreateEntry(Path.GetFileName(image.Name), CompressionLevel.Fastest);

                    using (var entryStream = entry.Open())
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        await responseStream.CopyToAsync(entryStream);
                    }
                }
            }

            archiveStream.Position = 0; // Сбрасываем позицию потока
            return File(archiveStream.ToArray(), "application/zip", "images.zip");
        }

        [HttpGet("docfile/{id:int}")]
        public async Task<IActionResult> GetDocumentFile(int id)
        {
            var parentDocumentResponse = await _model.GetDocumentById(id);
            if (parentDocumentResponse.HttpStatus != 200 || parentDocumentResponse.Value == null)
            {
                return BadRequest($"Error {parentDocumentResponse.HttpStatus} occurs while search parent document: {parentDocumentResponse.Message}");
            }
            var parentDocument = parentDocumentResponse.Value;
            if (parentDocument == null || parentDocument.ContentLink == null || parentDocument.ContentLink.Length == 0)
            {
                return NotFound();
            }
            var response = await _http.GetAsync($"{_servicesOptions.DocumentService}/DocumentProcessing/doc?link={parentDocument.ContentLink}");
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Cannot Load Doc");
            }
            byte[] fileBytes;
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                var ms = new MemoryStream();
                responseStream.CopyTo(ms);
                fileBytes = ms.ToArray();
                return File(fileBytes, $"application/{parentDocument.Extension.ToLower()}");
            }
        }

        [HttpPost("generate/{id:int}")]
        public async Task<IActionResult> GenerateDoc(int id)
        {
            try
            {
                if (!Request.HasFormContentType)
                    return BadRequest("Expected form-data request");
                var form = await Request.ReadFormAsync();

                // Обработка архива с изображениями
                var imagesFile = form.Files["images"];
                if (imagesFile == null || imagesFile.Length == 0)
                    return BadRequest("Images archive is required");

                // Обработка данных о выделениях
                var selectionsJson = form["selections"];
                if (string.IsNullOrEmpty(selectionsJson))
                    return BadRequest("Selections data is required");

                var parentDocumentResponse = await _model.GetDocumentById(id);
                if (parentDocumentResponse.HttpStatus != 200 || parentDocumentResponse.Value == null)
                {
                    return BadRequest($"Error {parentDocumentResponse.HttpStatus} occurs while search parent document: {parentDocumentResponse.Message}");
                }
                var parentDocument = parentDocumentResponse.Value;

                // Десериализация выделений
                var selections = JsonConvert.DeserializeObject<List<ImageSelection>>(selectionsJson);
                int counter = 0;
                while (counter < selections.Count)
                {
                    var selection = selections[counter];
                    if (selection.Points.Count != 4)
                    {
                        selections.Remove(selection);
                    }
                    int firstX = (int)Math.Round(selection.Points[0].X);
                    int firstY = (int)Math.Round(selection.Points[0].Y);
                    if (selection.Points.All(point => (int)Math.Round(point.X) == firstX) || selection.Points.All(point => (int)Math.Round(point.Y) == firstY))
                    {
                        selections.Remove(selection);
                    }
                    else if (selections.Count(x => x.Id == selection.Id && x.Points[0].Y == selection.Points[0].Y) > 1)
                    {
                        selections.Remove(selection);
                    }
                    else
                    {
                        counter++;
                    }
                    
                }
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                try
                {
                    // Сохраняем и распаковываем архив
                    var archivePath = Path.Combine(tempFolder, "images.zip");
                    using (var stream = System.IO.File.Create(archivePath))
                    {
                        await imagesFile.CopyToAsync(stream);
                    }

                    ZipFile.ExtractToDirectory(archivePath, tempFolder);

                    // Получаем список изображений
                    var imageFiles = Directory.GetFiles(tempFolder)
                        .Where(f => !f.EndsWith(".zip"))
                        .OrderBy(f => f)
                        .ToList();

                    var docMarkup = new List<DocMarkup>();
                    // Обрабатываем каждое изображение
                    for (int i = 0; i < imageFiles.Count; i++)
                    {
                        var imagePath = imageFiles[i];
                        var imageSelections = selections?
                            .Where(s => s.Id == i)
                            .ToList();

                        var processResult = await ProcessImageAsync(imagePath, i, imageSelections);
                        if (processResult == null)
                        {
                            throw new Exception("Image split failure");
                        }
                        
                        for (int j = 0; j < processResult.Segments.Count; j++) 
                        {
                            string text = "";
                            try
                            {

                                byte[] imageBytes = processResult.Segments[j];

                                // Создаем контент из байтов
                                var imageContent = new ByteArrayContent(imageBytes);
                                imageContent.Headers.ContentType =
                                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/png");

                                // Отправка запроса
                                var textResponse = await _http.PostAsync(
                                    "http://localhost:8000/recognize-text/",
                                    imageContent
                                );
                                // Читаем результат
                                text = await textResponse.Content.ReadAsStringAsync();


                                
                            }
                            catch (Exception ex)
                            {
                                BadRequest(ex);
                            }
                            docMarkup.Add(new DocMarkup() { Type = MarkupType.Text, Content = text });
                            if (processResult.CroppedRegions.Count > j)
                            {
                                docMarkup.Add(new DocMarkup() { Content = processResult.CroppedRegions[j], Type = MarkupType.Image });
                            }
                        }
                    }
                    var response = await _http.PostAsJsonAsync<List<DocMarkup>>($"{_servicesOptions.DocumentService}/DocumentProcessing/{parentDocument.Extension.ToLower()}?fileDirectory={parentDocument.User.Id}/{parentDocument.id}&fileName={parentDocument.Name}", docMarkup);
                    if (response.IsSuccessStatusCode)
                    {
                        string link = await response.Content.ReadAsStringAsync();
                        await _model.UpdateDocumentLink(parentDocument.id, link);
                    }
                    else
                    {
                        throw new Exception("Document generate failure");
                    }
                }
                finally
                {
                    // Удаляем временные файлы
                    Directory.Delete(tempFolder, true);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            
        }

        private async Task<ProcessingResultResponse> ProcessImageAsync(
            string imagePath,
            int imageIndex,
            List<ImageSelection> selections)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                // Добавляем изображение
                var imageContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(imagePath));
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                content.Add(imageContent, "image", $"image_{imageIndex}.png");

                // Добавляем селекты
                if (selections?.Any() == true)
                {
                    var selectionsJson = JsonConvert.SerializeObject(selections);
                    content.Add(new StringContent(selectionsJson), "selections");
                }

                // Отправляем запрос
                var response = await _http.PostAsync($"{_servicesOptions.ImageService}/Image/split", content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ProcessingResultResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
