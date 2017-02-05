using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.EF;
using AirShow.Models.Common;
using System.IO;

namespace AirShow.Models.Interfaces
{
    public interface IPresentationThumbnailRepository
    {
        Task<OperationStatus> AddThumbnailFor(string fileID, Stream fileStream);
        Task<OperationStatus> RemoveThumbnailFor(string fileID);
        Task<OperationResult<string>> GetThumbnailURLFor(string fileID);
    }
}
