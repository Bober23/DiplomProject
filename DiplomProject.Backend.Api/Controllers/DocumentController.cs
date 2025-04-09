using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Drawing;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public DocumentController(IDocumentModel model, HttpClient client)
        {
            _model = model;
            _http = client;
        }

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

        [HttpPatch("namecat/{id:int}")]
        public async Task<IActionResult> UpdateDocumentName(int id, [FromBody]DocumentRenameRequest request)
        {
            var response = await _model.UpdateDocumentName(id, request.Name);
            var response2 = await _model.UpdateDocumentCategory(id, request.Category);
            if (response.HttpStatus == 200 && response2.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

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

            // Добавляем файл в форму
            formData.Add(fileContent, "file", file.FileName);

            // Отправляем POST-запрос
            var response = await _http.PostAsync($"http://localhost:5127/Image?fileDirectory={parentDocument.User.Id}/{parentDocument.id}/images", formData);
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
                    response = await _http.GetAsync($"http://localhost:5127/Image?linkToFile={image.LinkToFile}&binarized={binarized}");
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
                Console.WriteLine(imagesFile.ToString());
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            
        }
    }
}
