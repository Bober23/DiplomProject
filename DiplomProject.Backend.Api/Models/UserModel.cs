using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DiplomProject.Backend.Api.Models
{
    public class UserModel : Model, IUserModel
    {
        public UserModel(DataContext dbContext) : base(dbContext) { }

        public async Task<ModelResponse<User>> AddNewUser(UserParametersRequest request)
        {
            if (request == null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 400, Message = "EmptyRequest" };
            }
            if (!VerifyEmail(request.Email))
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 400, Message = "Incorrect Email" };
            }
            if (_dbContext.Users.FirstOrDefault(x => x.Email == request.Email) != null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 409, Message = "User Is Already Exist" };
            }
            var user = new User { Email = request.Email, PasswordHash = request.PasswordHash };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            user = _dbContext.Users.FirstOrDefault(x => x.Email == request.Email);
            return new ModelResponse<User> { Value = user, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<User>> DeleteUser(int id)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 404, Message = "No User Found" };
            }
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<User> { Value = user, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<List<User>>> GetAllUsers()
        {
            var users = _dbContext.Users.ToList();
            return new ModelResponse<List<User>>() { Value = users, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<User>> GetUserByEmail(string email)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 404, Message = "No User Found" };
            }
            return new ModelResponse<User> { Value = user, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<User>> GetUserById(int id)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 404, Message = "No User Found" };
            }
            return new ModelResponse<User> { Value = user, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<User>> TryToLogin(UserParametersRequest request)
        {
            if (request == null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 400, Message = "EmptyRequest" };
            }
            User? user = _dbContext.Users.FirstOrDefault(x => x.Email == request.Email);
            if (user == null || user.PasswordHash != request.PasswordHash)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 400, Message = "Incorrect User Data" };
            }
            return new ModelResponse<User> { Value = user, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<User>> UpdateUserPassword(int id, string newPasswordHash)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                return new ModelResponse<User> { Value = null, HttpStatus = 404, Message = "No User Found" };
            }
            user.PasswordHash = newPasswordHash;
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<User> { Value = user, HttpStatus = 200, Message = "OK" };
        }

        private bool VerifyEmail(string email)
        {
            bool isEmailCorrect = true;
            //TODO: прикрутить логику проверки существования емаила
            return isEmailCorrect;
        }
    }
}
