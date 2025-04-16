using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiplomProject.Backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BugReportController : ControllerBase
    {
        private readonly DataContext _model;
        public BugReportController(DataContext model, HttpClient client, IOptions<ServicesOptions> options)
        {
            _model = model;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBugReport([FromBody] BugReportRequest request)
        {
            var report = new BugReport()
            {
                DateTime = DateTime.Now,
                Text = request.Message
            };
            Console.WriteLine("SHIT");
            _model.BugReports.Add(report);
            await _model.SaveChangesAsync();
            return Ok(report);
        }
    }
}
