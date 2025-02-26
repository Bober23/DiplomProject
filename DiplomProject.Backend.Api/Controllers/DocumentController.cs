using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;

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

        [HttpDelete]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var response = await _model.DeleteDocument(id);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpPatch("name/{id:int}")]
        public async Task<IActionResult> UpdateDocumentName(int id, [FromQuery] string name)
        {
            var response = await _model.UpdateDocumentName(id, name);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpPatch("category/{id:int}")]
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
            var response = await _http.PostAsync($"http://localhost:5127/Image?fileDirectory={parentDocument.User.Id}/{parentDocument.id}", formData);
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
    }
}
