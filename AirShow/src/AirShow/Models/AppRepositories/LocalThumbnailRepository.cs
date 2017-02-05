using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Common;
using AirShow.Models.EF;
using AirShow.Models.Interfaces;
using System.Diagnostics;

namespace AirShow.Models.AppRepositories
{
    public class LocalThumbnailRepository : IPresentationThumbnailRepository
    {

        public async Task<OperationStatus> RemoveThumbnailFor(string fileID)
        {
            var opStatus = new OperationStatus();
            try
            {
                var path = GeneratePhysicalPath(fileID);
                File.Delete(path);
            } catch(Exception e)
            {
                opStatus.ErrorMessageIfAny = "File not found";
            }

            return opStatus;
        }


        public async Task<OperationStatus> AddThumbnailFor(string fileID, Stream fileStream)
        {
            var opStatus = new OperationStatus();

            AirshowUtils.ConfirmDirectoryExistsOrCreate(ImagesDirectory);
            fileStream.Seek(0, SeekOrigin.Begin);

            var pdfInput = GeneratePathForFileIdWithExtension(fileID, "pdf");
            var jpegOutput = GeneratePathForFileIdWithExtension(fileID, "jpeg");

            using (var phyisicalFileStream = File.Create(pdfInput))
            {
                await fileStream.CopyToAsync(phyisicalFileStream);
            }

            System.Diagnostics.Process clientProcess = new Process();
            clientProcess.StartInfo.FileName = "java";
            clientProcess.StartInfo.Arguments = @"-jar " + JarPath + " " + $"PDFThumbGenerator {pdfInput} {jpegOutput}";
            clientProcess.Start();
            clientProcess.WaitForExit();
            File.Delete(pdfInput);

            int code = clientProcess.ExitCode;
            if (code != 0)
            {
                opStatus.ErrorMessageIfAny = "An error ocurred during the generation of the image";
            }


            return opStatus;
        }

        public async Task<OperationResult<string>> GetThumbnailURLFor(string fileID)
        {
            return new OperationResult<string>
            {
                Value = GenerateThumbnailPath(fileID)
            };
        }


        public static string GeneratePathForFileIdWithExtension(string fileID, string extension)
        {
            
            return $"{ImagesDirectory}{Path.DirectorySeparatorChar}{fileID}.{extension}";
        }

        public static string GenerateThumbnailPath(string fileID)
        {
            return $"/images/{fileID}.jpeg";
        }

        public static string GeneratePhysicalPath(string fileID)
        {
            var path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "wwwroot" + 
                Path.DirectorySeparatorChar + "images" + Path.DirectorySeparatorChar + fileID + ".jpeg";
            return path;
        }

        public static string ImagesDirectory
        {
            get
            {
                return $"wwwroot{Path.DirectorySeparatorChar}images";
            }
        }

        public static string JarPath
        {
            get
            {
                return $"ThumbGenerator{Path.DirectorySeparatorChar}PDFThumbnailGenerator.jar";
            }
        }
    }
}
