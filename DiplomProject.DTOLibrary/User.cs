namespace DiplomProject.DTOLibrary
{
    public class User
    {
        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public int Id { get; set; }

        public virtual List<Document> Documents { get; set; }
    }
}