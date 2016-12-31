using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Common;
using AirShow.Models.EF;
using AirShow.Models.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AirShow.Models.AppRepositories
{
    public class EFTagsRepository : ITagsRepository
    {
        private AirShowContext _context;

        public EFTagsRepository(AirShowContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<List<Tag>>> CreateOrGetTags(List<string> tagsAsStrings)
        {
            var opRes = new OperationResult<List<Tag>>();
            List<Tag> result = new List<Tag>();
            opRes.Value = result;

            if (tagsAsStrings == null || tagsAsStrings.Count == 0)
            {
                opRes.ErrorMessageIfAny = "Invalid tag names specified";
                return opRes;
            }

            List<Tag> existentTags = _context.Tags.Where(t => tagsAsStrings.Contains(t.Name)).ToList();
            if (existentTags != null && existentTags.Count > 0)
            {
                foreach (var tag in existentTags)
                {
                    if (tag.PresentationTags == null)
                    {
                        tag.PresentationTags = new List<PresentationTag>();
                    }
                }
                result.AddRange(existentTags);
            }

            foreach (var item in tagsAsStrings)
            {
                if (!existentTags.Any(t => t.Name == item))
                {
                    var newTag = new Tag
                    {
                        Name = item,
                        PresentationTags = new List<PresentationTag>()
                    };
                    _context.Tags.Add(newTag);
                    result.Add(newTag);
                }
            }

            await _context.SaveChangesAsync();
            return opRes;
        }

       public async  Task<OperationStatus> AddTagsForPresentation(List<string> tags, Presentation p)
        {
            return new OperationStatus { };
        }

        
        public async Task<OperationResult<List<Tag>>> GetTagsForPresentation(Presentation p)
        {
            return new OperationResult<List<Tag>>
            {
                Value = await _context.PresentationTags.
                                Where(pt => pt.PresentationId == p.Id).
                                Include(pt => pt.Tag).
                                Select(pt => pt.Tag).
                                ToListAsync()
            };
        }

        public async Task<OperationStatus> RemoveTagFromPresentation(string tag, Presentation p)
        {
            throw new NotImplementedException();
        }
    }

         
    
}
