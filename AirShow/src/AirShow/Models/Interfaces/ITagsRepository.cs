using AirShow.Models.Common;
using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Interfaces
{
    public interface ITagsRepository
    {
        Task<OperationResult<List<Tag>>> GetTagsForPresentation(Presentation p);
        Task<OperationStatus> RemoveTagFromPresentation(string tag, Presentation p);
        Task<OperationStatus> AddTagsForPresentation(List<string> tags, Presentation p);
        Task<OperationResult<List<Tag>>> CreateOrGetTags(List<string> tagsAsStrings);
    }
}
