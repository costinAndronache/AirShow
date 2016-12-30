using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.EF;
using System.IO;
using AirShow.Models.Contexts;
using AirShow.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace AirShow.Models
{
    public class EFRepository : IAppRepository
    {
        private AirShowContext _context;
        private IPresentationFilesRepository _filesRepository;

        public EFRepository(AirShowContext context, IPresentationFilesRepository filesRepository)
        {
            _context = context;
            _filesRepository = filesRepository;
        }

        public async Task<OperationStatus> DeletePresentation(string presentationName, string userId)
        {
            var opResult = new OperationStatus();

            var list = _context.Presentations.Where(p => p.Name == presentationName && p.UserId == userId).ToList();
            if (list.Count == 1)
            {
                var presentation = list.First();
                var presentationTags = _context.PresentationTags.Where(pt => pt.PresentationId == presentation.Id).ToList();
                _context.Presentations.Remove(presentation);
                _context.PresentationTags.RemoveRange(presentationTags);
                 _context.SaveChanges();

                return await _filesRepository.DeleteFileForUser(presentationName, userId);

            } else
            {
                opResult.ErrorMessageIfAny = "File not found";
            }
            return opResult;
        }

        public async Task<OperationResult<List<Category>>> GetCurrentCategories()
        {
            return new OperationResult<List<Category>>
            {
               Value =  _context.Categories.ToList()
            };
        }

        public async Task<OperationResult<List<Presentation>>> GetPresentationsForUser(string userId)
        {
            return new OperationResult<List<Presentation>>
            {
                Value = _context.Presentations.Where(p => p.UserId == userId).ToList()
            };
        }

        public async Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream)
        {
            return await _filesRepository.GetFileForUser(name, userId, inStream);
        }

        public async Task<OperationStatus> UploadPresentationForUser(string name, string description, string userId, int categoryId, List<string> tags, Stream stream)
        {
            if(_context.Presentations.Any(p => p.UserId == userId && p.Name == name))
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kPresentationWithSameNameExists };
            }

            if (!_context.Categories.Any(c => c.Id == categoryId))
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kNoSuchCategoryWithId };
            }

            var tagsForPresentation = await CreateOrGetTags(tags);
            var currentPresentation = new Presentation
            {
                Name = name,
                Description = description,
                UserId = userId,
                CategoryId = categoryId,
                PresentationTags = new List<PresentationTag>()
            };

            await _context.Presentations.AddAsync(currentPresentation);

            foreach (var tag in tagsForPresentation)
            {
                var pt = new PresentationTag
                {
                    Tag = tag,
                    Presentation = currentPresentation
                };

                tag.PresentationTags.Add(pt);
                currentPresentation.PresentationTags.Add(pt);
               
            }

            int rows = await _context.SaveChangesAsync();
            if (rows > 0)
            {
                return await _filesRepository.SaveFileForUser(stream, name, userId);
            }

            return new OperationStatus() {ErrorMessageIfAny = OperationStatus.kUnknownError };
        }



        public async Task<OperationResult<List<Tag>>> GetTagsForPresentation(Presentation p)
        {
            var pts = _context.PresentationTags.Where(pt => pt.PresentationId == p.Id).Include(pt => pt.Tag).ToList();
            var list = new List<Tag>();
            foreach (var item in pts)
            {
                list.Add(item.Tag);
            }

            return new OperationResult<List<Tag>>
            {
                Value = list
            };
        }

        private async Task<List<Tag>> CreateOrGetTags(List<string> tagsAsStrings)
        {
            List<Tag> result = new List<Tag>();

            if (tagsAsStrings == null || tagsAsStrings.Count == 0)
            {
                return result;
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
            return result;
        }

        
    }
}
