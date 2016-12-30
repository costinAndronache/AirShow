using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using AirShow.Models.Common;

namespace AirShow.Models.Interfaces
{
    public interface IPresentationFilesRepository
    {
        Task<OperationStatus> GetFileForUser(string filename, string userId, Stream inStream);
        Task<OperationStatus> SaveFileForUser(Stream fileStream, string filename, string userId);
        Task<OperationStatus> DeleteFileForUser(string filename, string userId);
    }


}
