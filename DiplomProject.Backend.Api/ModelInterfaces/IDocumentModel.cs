using DiplomProject.Backend.Api.Models;
using DiplomProject.Backend.Api.Requests;
using DiplomProject.DTOLibrary;

namespace DiplomProject.Backend.Api.Models
{
    public interface IDocumentModel
    {
        public Task<ModelResponse<Document>> CreateNewDocument(DocumentParameterRequest request);
        public Task<ModelResponse<Document>> UpdateDocumentCategory(int id, string category);
        public Task<ModelResponse<Document>> UpdateDocumentName(int id, string name);
        public Task<ModelResponse<Document>> DeleteDocument(int id);
        public Task<ModelResponse<Document>> GetDocumentById(int id);
        public Task<ModelResponse<DocFile>> AddDocFileToDocument(DocFile docFile);
    }
}
