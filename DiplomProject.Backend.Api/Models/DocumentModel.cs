using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;
using Microsoft.EntityFrameworkCore;

namespace DiplomProject.Backend.Api.Models
{
    public class DocumentModel : Model, IDocumentModel
    {
        public DocumentModel(DataContext dbContext) : base(dbContext) { }

        public async Task<ModelResponse<DocFile>> AddDocFileToDocument(DocFile docFile)
        {
            if (docFile == null)
            {
                return new ModelResponse<DocFile> { Value = null, HttpStatus = 400, Message = "EmptyRequest" };
            }
            _dbContext.ImageFiles.Add(docFile);
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<DocFile> {Value = docFile, HttpStatus = 200, Message = "OK"};
        }

        public async Task<ModelResponse<Document>> CreateNewDocument(DocumentParameterRequest request)
        {
            if (request == null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 400, Message = "EmptyRequest" };
            }
            if (_dbContext.Documents.FirstOrDefault(x => x.Name == request.Name) != null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 409, Message = "Document with this name is already exist" };
            }
            User? author = _dbContext.Users.FirstOrDefault(x => x.Id == request.AuthorId);
            if (author == null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 400, Message = "Author not found" };
            }
            var document = new Document { Name = request.Name, Extension = request.Extension, Category = request.Category, User = author };
            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<Document> { Value = document, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<Document>> DeleteDocument(int id)
        {
            var document = _dbContext.Documents.FirstOrDefault(x => x.id == id);
            if (document == null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 400, Message = "Document not found" };
            }
            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<Document> { Value = document, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<Document>> GetDocumentById(int id)
        {
            var document = _dbContext.Documents.FirstOrDefault(x => x.id == id);
            if (document == null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 400, Message = "Document not found" };
            }
            return new ModelResponse<Document> { Value = document, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<Document>> UpdateDocumentCategory(int id, string category)
        {
            var document = _dbContext.Documents.FirstOrDefault(x => x.id == id);
            if (document == null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 400, Message = "Document not found" };
            }
            document.Category = category;
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<Document> { Value = document, HttpStatus = 200, Message = "OK" };
        }

        public async Task<ModelResponse<Document>> UpdateDocumentName(int id, string name)
        {
            var document = _dbContext.Documents.FirstOrDefault(x => x.id == id);
            if (document == null)
            {
                return new ModelResponse<Document> { Value = null, HttpStatus = 400, Message = "Document not found" };
            }
            document.Name = name;
            await _dbContext.SaveChangesAsync();
            return new ModelResponse<Document> { Value = document, HttpStatus = 200, Message = "OK" };
        }

        
    }
}
