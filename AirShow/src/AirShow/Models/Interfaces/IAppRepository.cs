using AirShow.Models.Common;
using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Interfaces
{
    public interface IAppRepository
    {
        Task<OperationResult<List<Presentation>>> GetPresentationsForUser(string userId);
        Task<OperationStatus> UploadPresentationForUser(string name, 
            string description, string userId, int categoryId, List<string> tags, Stream stream);

        Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream);
        Task<OperationResult<List<Category>>> GetCurrentCategories();
        Task<OperationStatus> DeletePresentation(string presentationName, string userId);

        Task<OperationResult<List<Tag>>> GetTagsForPresentation(Presentation p);
    }
}
