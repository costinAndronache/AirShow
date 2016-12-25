﻿using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Common;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace AirShow.Models.FileRepositories
{
    public class BasicFileRepository : IPresentationFilesRepository
    {
        private IHostingEnvironment _env;

        public BasicFileRepository(IHostingEnvironment env)
        {
            _env = env;
        }

        public async Task<OperationResult> GetFileForUser(string filename, string userId, Stream inStream)
        {
            var path = BuildPathFor(userId, filename);
            if (!File.Exists(path))
            {
                return new OperationResult
                {
                    ErrorMessageIfAny = OperationResult.kUnknownError
                };
            }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                await fs.CopyToAsync(inStream);
            }

            return new OperationResult();
        }

        public async Task<OperationResult> SaveFileForUser(Stream fileStream, string filename, string userId)
        {
            var directoryExistsOrCreatedResult = ConfirmDirectoryExistsOrCreate(userId);
            if (directoryExistsOrCreatedResult != null)
            {
                return directoryExistsOrCreatedResult;
            }

            var filePath = BuildPathFor(userId, filename);
            var fs = CreateFileToWriteAtPath(filePath);
            if (fs == null)
            {
                return new OperationResult
                {
                    ErrorMessageIfAny = OperationResult.kInvalidFileNameOrAlreadyExists
                };
            }

            await fileStream.CopyToAsync(fs);
            fs.Dispose();
            return new OperationResult();
        }


        private static string BuildDirectoryPathFor(string userId)
        {
            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "userUploads" + Path.DirectorySeparatorChar + userId;
        }

        private static string BuildPathFor(string userId, string filename)
        {
            return BuildDirectoryPathFor(userId) + Path.DirectorySeparatorChar + filename;
        }

        private OperationResult ConfirmDirectoryExistsOrCreate(string userId)
        {
            var directoryPath = BuildDirectoryPathFor(userId);
            
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    var di = Directory.CreateDirectory(directoryPath);
                }
                catch (Exception e)
                {
                    var message = OperationResult.kUnknownError;
                    if (_env.IsDevelopment())
                    {
                        message = e.InnerException.ToString();
                    }
                    return new OperationResult
                    {
                        ErrorMessageIfAny = message
                    };
                }
            }

            return null;
        }

        private static FileStream CreateFileToWriteAtPath(string path)
        {
            try
            {
                FileStream fs = new FileStream(path, FileMode.CreateNew);
                return fs;
            } catch (Exception e)
            {
                return null;
            }
        }
    }
}