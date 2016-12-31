using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Interfaces;
using AirShow.Models.Contexts;
using AirShow.Models.EF;
using AirShow.Models.Common;
using System.IO;

namespace AirShow.Models.AppRepositories
{
    public class EFPresentationsRepository : IPresentationsRepository
    {
        private AirShowContext _context;
        private IPresentationFilesRepository _filesRepository;
        private ITagsRepository _tagsRepository;

        public EFPresentationsRepository(AirShowContext context,
                                         IPresentationFilesRepository filesRepository,
                                         ITagsRepository tagsRepository)
        {
            _context = context;
            _filesRepository = filesRepository;
            _tagsRepository = tagsRepository;
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

            }
            else
            {
                opResult.ErrorMessageIfAny = "File not found";
            }
            return opResult;
        }



        public async Task<OperationResult<int>> GetNumberOfPresentationsForUser(string userId)
        {
            return new OperationResult<int>
            {
                Value = _context.Presentations.Count(pt => pt.UserId == userId)
            };
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetPresentationsForUser(string userId, PagingOptions options)
        {
            return new PagedOperationResult<List<Presentation>>
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
            if (_context.Presentations.Any(p => p.UserId == userId && p.Name == name))
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kPresentationWithSameNameExists };
            }

            if (!_context.Categories.Any(c => c.Id == categoryId))
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kNoSuchCategoryWithId };
            }

            var tagsForPresentationResult = await _tagsRepository.CreateOrGetTags(tags);
            if (tagsForPresentationResult.ErrorMessageIfAny != null)
            {
                return tagsForPresentationResult;
            }
            var tagsForPresentation = tagsForPresentationResult.Value;

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

            return new OperationStatus() { ErrorMessageIfAny = OperationStatus.kUnknownError };
        }

    }
       
}
