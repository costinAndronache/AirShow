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
        Task<List<Presentation>> GetPresentationsForUser(string userId);
        Task<OperationResult> UploadPresentationForUser(string name, 
            string description, string userId, int categoryId, List<string> tags, Stream stream);

        Task<OperationResult> DownloadPresentation(string name, string userId, Stream inStream);
        Task<List<Category>> GetCurrentCategories();

    }
}
