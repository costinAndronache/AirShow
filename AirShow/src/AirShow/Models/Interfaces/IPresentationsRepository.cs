using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.EF;
using AirShow.Models.Common;
using System.IO;

namespace AirShow.Models.Interfaces
{
    public interface IPresentationsRepository
    {
        Task<PagedOperationResult<List<Presentation>>> GetPresentationsForUser(string userId, PagingOptions options);
        Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromCategory(string categoryName, string userId, PagingOptions options);
        Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromTag(string tag, string userId, PagingOptions options);
        Task<OperationResult<int>> GetNumberOfUserPresentationsInCategory(string categoryName, string userId);
        Task<OperationResult<int>> GetNumberOfUserPresentationsWithTag(string tag, string userId);



        Task<OperationResult<int>> GetNumberOfPresentationsForUser(string userId);
        Task<OperationStatus> UploadPresentationForUser(string name,
        string description, string userId, int categoryId, List<string> tags, Stream stream);
        Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream);
        Task<OperationStatus> DeletePresentation(string presentationName, string userId);
    }
}
