using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Mvc;

namespace DiplomProject.Backend.Api.Models
{
    public interface IUserModel
    {
        public Task<ModelResponse<User>> AddNewUser(UserParametersRequest request);
        public Task<ModelResponse<User>> UpdateUserPassword(int id, string newPasswordHash);
        public Task<ModelResponse<User>> DeleteUser(int id);
        public Task<ModelResponse<List<User>>> GetAllUsers();
        public Task<ModelResponse<User>> GetUserByEmail(string email);
        public Task<ModelResponse<User>> GetUserById(int id);
        public Task<ModelResponse<User>> TryToLogin(UserParametersRequest request);
    }
}
