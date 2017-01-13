using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using AirShow.Models.Common;
using AirShow.Models.EF;

namespace AirShow.Models.Interfaces
{
    public interface IPresentationFilesRepository
    {
        Task<OperationStatus> GetFileForId(string fileID, Stream inStream);
        Task<OperationResult<string>> SaveFile(Stream fileStream);
        Task<OperationStatus> DeleteFileWithId(string fileID);
    }


}
