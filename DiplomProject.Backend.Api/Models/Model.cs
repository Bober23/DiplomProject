namespace DiplomProject.Backend.Api.Models
{
    public class Model
    {
        protected readonly DataContext _dbContext;

        public Model(DataContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
