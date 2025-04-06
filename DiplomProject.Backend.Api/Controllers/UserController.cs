
using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiplomProject.Backend.Api
{
    //TODO: возвращать правильные статусы ошибок, а не BadRequest
    [ApiController]
    [Route("api/[controller]")]
    public class UserController:ControllerBase
    {
        private readonly IUserModel _model;
        public UserController(IUserModel model)
        {
            _model = model;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = await _model.GetAllUsers();
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpGet("byId/{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var response = await _model.GetUserById(id);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpGet("byEmail")]
        public async Task<IActionResult> GetUserById([FromQuery] string email)
        {
            var response = await _model.GetUserByEmail(email);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpPost("login")]
        public async Task<IActionResult> TryToLogin(UserParametersRequest request)
        {
            var response = await _model.TryToLogin(request);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return Unauthorized();
        }

        [HttpPost("new")]
        public async Task<IActionResult> AddNewUser(UserParametersRequest request)
        {
            var response = await _model.AddNewUser(request);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> UpdateUserPassword(int id, [FromQuery] string passwordHash)
        {
            var response = await _model.UpdateUserPassword(id, passwordHash);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var response = await _model.DeleteUser(id);
            if (response.HttpStatus == 200)
            {
                return Ok(response.Value);
            }
            return BadRequest(response.Message);
        }
    }
}
