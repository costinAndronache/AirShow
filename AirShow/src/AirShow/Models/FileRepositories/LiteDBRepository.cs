using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LiteDB;
using AirShow.Models.Common;

namespace AirShow.Models.FileRepositories
{
    public class LiteDBRepository : IPresentationFilesRepository, IDisposable
    {
        private LiteDatabase _liteDb;
        public LiteDBRepository()
        {
            _liteDb = new LiteDatabase("../../Databases/PresentationFiles.db");
        }

        public void Dispose()
        {
            _liteDb.Dispose();
        }

        public async Task<OperationResult> GetFileForUser(string filename, string userId, Stream inStream)
        {
            _liteDb.FileStorage.Download("/" + userId + "/" + filename, inStream);
            return new OperationResult(); 
        }

        public async Task<OperationResult> SaveFileForUser(Stream fileStream, string filename, string userId)
        {
            _liteDb.FileStorage.Upload("/" + userId + "/" + filename, fileStream);
            return new OperationResult();
        }
    }

}
