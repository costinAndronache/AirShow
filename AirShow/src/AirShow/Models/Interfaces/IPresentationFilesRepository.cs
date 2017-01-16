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
        Task<OperationStatus> GetFileForId(int fileID, Stream inStream);
        Task<OperationResult<int>> SaveFile(Stream fileStream);
        Task<OperationStatus> DeleteFileWithId(int fileID);
    }


}
