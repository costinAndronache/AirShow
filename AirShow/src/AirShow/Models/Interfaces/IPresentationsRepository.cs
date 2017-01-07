using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.EF;
using AirShow.Models.Common;
using System.IO;

namespace AirShow.Models.Interfaces
{
    public class UploadPresentationModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public int CategoryId { get; set; }
        public Stream SourceStream { get; set; }
        public bool IsPublic { get; set; }
    }

    public enum PresentationSearchType
    {
        None = 0,
        Name = 1, 
        Description = 2,
        Tags = 4 
    }

    public interface IPresentationsRepository
    {


        Task<PagedOperationResult<List<Presentation>>> GetPresentationsForUser(string userId, PagingOptions options);
        Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromCategory(string categoryName, string userId, PagingOptions options);
        Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromTag(string tag, string userId, PagingOptions options);
        Task<OperationResult<int>> GetNumberOfUserPresentationsInCategory(string categoryName, string userId);
        Task<OperationResult<int>> GetNumberOfUserPresentationsWithTag(string tag, string userId);

        Task<PagedOperationResult<List<Presentation>>> SearchUserPresentations(List<string> keywords, string userId, PagingOptions options,
                                                                               PresentationSearchType searchType);

        Task<OperationResult<int>> GetNumberOfPresentationsForUser(string userId);
        Task<OperationStatus> UploadPresentationForUser(string userId, UploadPresentationModel model);
        Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream);
        Task<OperationStatus> DeletePresentation(string presentationName, string userId);
    }
}
