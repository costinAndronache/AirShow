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
        Task<OperationStatus> AddThumbnailFor(Presentation p, Stream fileStream);
        Task<OperationResult<string>> GetThumbnailURLFor(Presentation p);
    }
}
