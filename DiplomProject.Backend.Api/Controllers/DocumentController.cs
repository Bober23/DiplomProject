using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DiplomProject.Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentModel _model;
        public DocumentController(IDocumentModel model)
        {
            _model = model;
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
    }
}
