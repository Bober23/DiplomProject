namespace DiplomProject.Backend.Api.Models
{
    public class ModelResponse<T>
    {
        public T? Value { get; set; }
        public string? Message { get; set; }
        public int HttpStatus { get; set; }
    }
}
