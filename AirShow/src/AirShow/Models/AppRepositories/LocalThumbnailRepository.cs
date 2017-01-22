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
        public async Task<OperationStatus> AddThumbnailFor(Presentation p, Stream fileStream)
        {

            AirshowUtils.ConfirmDirectoryExistsOrCreate(ImagesDirectory);
            fileStream.Seek(0, SeekOrigin.Begin);

            var pdfInput = GeneratePathForPresentationWithExtension(p, "pdf");
            var jpegOutput = GeneratePathForPresentationWithExtension(p, "jpeg");

            using (var phyisicalFileStream = File.Create(pdfInput))
            {
                await fileStream.CopyToAsync(phyisicalFileStream);
            }

            System.Diagnostics.Process clientProcess = new Process();
            clientProcess.StartInfo.FileName = "java";
            clientProcess.StartInfo.Arguments = @"-jar " + JarPath + " " + $"program {pdfInput} {jpegOutput}";
            clientProcess.Start();
            clientProcess.WaitForExit();
            int code = (clientProcess.ExitCode);

            File.Delete(pdfInput);

            return new OperationStatus();
        }

        public async Task<OperationResult<string>> GetThumbnailURLFor(Presentation p)
        {
            return new OperationResult<string>
            {
                Value = GenerateThumbnailPath(p)
            };
        }

        public static string GenerateNameFor(Presentation p)
        {
            return p.UploadedDate.Ticks + "" + p.Id;
        }

        public static string GeneratePathForPresentationWithExtension(Presentation p, string extension)
        {
            var name = GenerateNameFor(p);
            return $"{ImagesDirectory}{Path.DirectorySeparatorChar}{name}.{extension}";
        }

        public static string GenerateThumbnailPath(Presentation p)
        {
            var name = GenerateNameFor(p);
            return $"/images/{name}.jpeg";
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
