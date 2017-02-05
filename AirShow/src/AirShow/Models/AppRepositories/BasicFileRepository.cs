using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Common;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using AirShow.Models.EF;
using System.Security.Cryptography;
using System.Text;
using AirShow.Models.Contexts;
using Microsoft.EntityFrameworkCore;
using AirShow.Utils;

namespace AirShow.Models.FileRepositories
{
    public class BasicFileRepository : IPresentationFilesRepository
    {
        private IHostingEnvironment _env;
        private static MD5 md5 = MD5.Create();
        private AirShowContext _context;
        private IPresentationThumbnailRepository _thumbnailsRepo;

        public BasicFileRepository(IHostingEnvironment env, 
                                   IPresentationThumbnailRepository thumbnailsRepo, 
                                   AirShowContext context)
        {
            _thumbnailsRepo = thumbnailsRepo;
            _context = context;
            _env = env;
        }

        private  OperationResult<string>  GetOrCreateUploadsDirectory()
        {
            var directory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "userUploads";
            var result = AirshowUtils.ConfirmDirectoryExistsOrCreate(directory);
            if (result.ErrorMessageIfAny != null)
            {
                return new OperationResult<string>
                {
                    ErrorMessageIfAny = result.ErrorMessageIfAny
                };
            }
            return new OperationResult<string>
            {
                Value = directory
            };
        }

        private OperationResult<string> CreatePathFor(string id)
        {
            var uploadsDirectoryResult = GetOrCreateUploadsDirectory();
            if (uploadsDirectoryResult.ErrorMessageIfAny != null)
            {
                return uploadsDirectoryResult;
            }

            return new OperationResult<string>
            {
                Value = uploadsDirectoryResult.Value + Path.DirectorySeparatorChar + id
            };
        }

        private string CreateIdFor(Stream fileStream)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            var hash = md5.ComputeHash(fileStream);
            var stringId = Encoding.ASCII.GetString(hash);
            stringId += DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return stringId;
        }


        public async Task<OperationStatus> GetFileForId(string id, Stream inStream)
        {
            var pathResult = CreatePathFor(id + "");
            if (pathResult.ErrorMessageIfAny != null)
            {
                return pathResult;
            }

            if (!File.Exists(pathResult.Value))
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = OperationStatus.kUnknownError
                };
            }

            using (FileStream fs = new FileStream(pathResult.Value, FileMode.Open))
            {
                await fs.CopyToAsync(inStream);
            }

            return new OperationStatus();
        }

        public async Task<OperationResult<string>> SaveFile(Stream fileStream)
        {
            var fileId = Guid.NewGuid().ToString("N");
            fileStream.Seek(0, SeekOrigin.Begin);
            var thumbResult = await _thumbnailsRepo.AddThumbnailFor(fileId, fileStream);

            if (thumbResult.ErrorMessageIfAny != null)
            {
                return new OperationResult<string>
                {
                    ErrorMessageIfAny = thumbResult.ErrorMessageIfAny
                };
            }


            var newFile = new PresentationFile();
            newFile.FileID = fileId;
            await _context.PresentationFiles.AddAsync(newFile);
            await _context.SaveChangesAsync();



            var filePathResult = CreatePathFor(fileId);

            if (filePathResult.ErrorMessageIfAny != null)
            {
                return new OperationResult<string>
                {
                    ErrorMessageIfAny = filePathResult.ErrorMessageIfAny
                };
            }

            var fs = AirshowUtils.CreateFileToWriteAtPath(filePathResult.Value);
            if (fs == null)
            {
                return new OperationResult<string>
                {
                    ErrorMessageIfAny = OperationStatus.kInvalidFileNameOrAlreadyExists
                };
            }
            fileStream.Seek(0, SeekOrigin.Begin);
            await fileStream.CopyToAsync(fs);


            fs.Dispose();

            return new OperationResult<string>
            {
                Value = newFile.FileID
            };
        }

        public async Task<OperationStatus> DeleteFileWithId(string id)
        {

            var fileFoundList = await _context.PresentationFiles.Where(pf => pf.FileID == id).ToListAsync();
            if (fileFoundList.Count != 1)
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = "No file found with the provided id"
                };
            }

            var file = fileFoundList.First();
            

            var res = new OperationStatus();

            if (!_context.Presentations.Any(p => p.FileID == id))
            {
                _context.PresentationFiles.Remove(file);
                
                var pathResult = CreatePathFor(id);
                if (pathResult.ErrorMessageIfAny != null)
                {
                    return pathResult;
                }

                if (!File.Exists(pathResult.Value))
                {
                    res.ErrorMessageIfAny = "The file does not exist or it has been deleted already.";
                }
                else
                {
                    File.Delete(pathResult.Value);
                    await _thumbnailsRepo.RemoveThumbnailFor(id);
                    var rows = await _context.SaveChangesAsync();
                    if (rows == 0)
                    {
                        res.ErrorMessageIfAny = "An error ocurred while trying to update the database";
                    }
                }
            }
            return res;
        }
    }
}
