using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Interfaces;
using AirShow.Models.Contexts;
using AirShow.Models.EF;
using AirShow.Models.Common;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace AirShow.Models.AppRepositories
{
    public class EFPresentationsRepository : IPresentationsRepository
    {
        private AirShowContext _context;
        private IPresentationFilesRepository _filesRepository;
        private ITagsRepository _tagsRepository;
        private IPresentationThumbnailRepository _thumbnailRepository;

        public EFPresentationsRepository(AirShowContext context,
                                         IPresentationFilesRepository filesRepository,
                                         ITagsRepository tagsRepository, 
                                         IPresentationThumbnailRepository thumbnailRepository)
        {
            _context = context;
            _filesRepository = filesRepository;
            _tagsRepository = tagsRepository;
            _thumbnailRepository = thumbnailRepository;
        }

        public async Task<OperationStatus> DeletePresentation(string presentationName, string userId)
        {
            var opResult = new OperationStatus();

            var upList = _context.UserPresentations.Where(up => up.UserId == userId);
            var singledOutUP = await upList.Where(up => _context.Presentations.Any(p => p.Id == up.PresentationId))
                .Include(up => up.Presentation).ToListAsync();

            if (singledOutUP.Count == 1)
            {
                var up = singledOutUP.First();
                var presentation = up.Presentation;

                var upToRemove = presentation.UserPresentations.Where(userp => userp.PresentationId == presentation.Id &&
                                                                            userp.UserId == userId).ToList();

                if (upToRemove.Count != 1)
                {
                    return new OperationStatus
                    {
                        ErrorMessageIfAny = "An error ocurred. The data for this presentation is corrupted"
                    };
                }

                presentation.UserPresentations.Remove(upToRemove.First());

                if (presentation.UserPresentations.Count == 0)
                {
                    var presentationTags = _context.PresentationTags.Where(pt => pt.PresentationId == presentation.Id).ToList();
                    _context.Presentations.Remove(presentation);
                    _context.PresentationTags.RemoveRange(presentationTags);
                    _context.SaveChanges();

                    return await _filesRepository.DeleteFileWithId(presentation.FileId);
                } else
                {
                    var rows = await _context.SaveChangesAsync();
                    if (rows > 0)
                    {
                        return new OperationStatus();
                    }
                    else
                    {
                        return new OperationStatus
                        {
                            ErrorMessageIfAny = "Unknown error"
                        };
                    }
                }


            }
            else
            {
                opResult.ErrorMessageIfAny = "File not found";
            }
            return opResult;
        }





        public async Task<PagedOperationResult<List<Presentation>>> GetPresentationsForUser(string userId, PagingOptions options)
        {
            var toSkip = (options.PageIndex - 1) * options.ItemsPerPage;
            var toTake = options.ItemsPerPage;

            var count = _context.UserPresentations.Count(up => up.UserId == userId);
            var totalPages = options.ItemsPerPage / (count > 0 ? count : options.ItemsPerPage);

            var upList = await _context.UserPresentations.Where(up => up.UserId == userId).Include(up => up.Presentation)
                .Select(up => up.Presentation).Skip(toSkip).Take(toTake).ToListAsync();


            return new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                TotalPages = totalPages,
                ItemsPerPage = options.ItemsPerPage
            };
        }

        public async Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream)
        {
            var upList = await _context.UserPresentations.Where(up => up.UserId == userId)
                .Where(up => _context.Presentations.Any(p => p.Id == up.PresentationId))
                .Include(up => up.Presentation).ToListAsync();

            if (upList.Count != 1)
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = "Presentation not found for this user"
                };
            }

            return await _filesRepository.GetFileForId(upList.First().Presentation.FileId, inStream);
        }


        public async Task<OperationStatus> UploadPresentationForUser(string userId, UploadPresentationModel model)
        {
            var upList = await _context.UserPresentations.Where(userPresentation => userPresentation.UserId == userId)
                .Include(userp => userp.Presentation).Where(userp => userp.Presentation.Name == model.Name).ToListAsync();

            if (upList.Count > 0)
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kPresentationWithSameNameExists };
            }

            if (!_context.Categories.Any(c => c.Id == model.CategoryId))
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kNoSuchCategoryWithId };
            }

            var tagsForPresentationResult = await _tagsRepository.CreateOrGetTags(model.Tags);
            if (tagsForPresentationResult.ErrorMessageIfAny != null)
            {
                return tagsForPresentationResult;
            }
            var tagsForPresentation = tagsForPresentationResult.Value;

            var currentPresentation = new Presentation
            {
                Name = model.Name,
                Description = model.Description,
                UserPresentations = new List<UserPresentation>(),
                CategoryId = model.CategoryId,
                PresentationTags = new List<PresentationTag>(),
                UploadedDate = DateTime.Now,
                IsPublic = model.IsPublic
            };

            var saveResult = await _filesRepository.SaveFile(model.SourceStream);
            if (saveResult.ErrorMessageIfAny != null)
            {
                return saveResult;
            }
            currentPresentation.FileId = saveResult.Value;

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

            var up = new UserPresentation
            {
                PresentationId = currentPresentation.Id,
                UserId = userId
            };

            _context.UserPresentations.Add(up);
            

            int rows = await _context.SaveChangesAsync();
            if (rows > 0)
            {
                var thumbnailResult = await _thumbnailRepository.AddThumbnailFor(currentPresentation, model.SourceStream);
                if (thumbnailResult.ErrorMessageIfAny != null)
                {
                    return thumbnailResult;
                }

                return new OperationStatus();
            }

            return new OperationStatus() { ErrorMessageIfAny = OperationStatus.kUnknownError };
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromCategory(string categoryName, string userId, PagingOptions options)
        {
            var upList = await _context.UserPresentations.Where(up => up.UserId == userId)
                .Include(up => up.Presentation)
                .Select(up => up.Presentation)
                .Include(p => p.Category)
                .Where(p => p.Category.Name == categoryName)
                .Skip(options.ToSkip)
                .Take(options.ItemsPerPage)
                .ToListAsync();

            
            return new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                ItemsPerPage = options.ItemsPerPage
            };
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromTag(string tag, string userId, PagingOptions options)
        {
            var upList = await _context.UserPresentations.Where(u => u.UserId == userId)
                .Include(u => u.Presentation)
                .Select(u => u.Presentation)
                .Include(p => p.PresentationTags)
                .Where(p => p.PresentationTags.Any(pt => pt.Tag.Name == tag))
                .Skip(options.ToSkip)
                .Take(options.ItemsPerPage)
                .ToListAsync();

            return new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                ItemsPerPage = options.ItemsPerPage
            };
        }

        public async Task<OperationResult<int>> GetNumberOfUserPresentationsInCategory(string categoryName, string userId)
        {
            var count =  _context.UserPresentations.Where(up => up.UserId == userId)
                                                         .Include(up => up.Presentation)
                                                         .Select(up => up.Presentation)
                                                         .Include(p => p.Category)
                                                         .Count(p => p.Category.Name == categoryName);


            var result = new OperationResult<int>
            {
                Value = count
            };

            return result;
        }

        public async Task<OperationResult<int>> GetNumberOfUserPresentationsWithTag(string tag, string userId)
        {
            var count = _context.UserPresentations.Where(u => u.UserId == userId)
                                                         .Include(u => u.Presentation)
                                                         .Select(u => u.Presentation)
                                                         .Include(p => p.PresentationTags)
                                                         .Count(p => p.PresentationTags.Any(pt => pt.Tag.Name == tag));


            return new OperationResult<int>
            {
                Value = count
            };
        }

        public async Task<PagedOperationResult<List<Presentation>>> SearchUserPresentations(List<string> keywords, string userId, PagingOptions options,
                                                                       PresentationSearchType searchType)
        {
            var presentationsResult = new List<Presentation>();
            var userPresentations = _context.UserPresentations.Where(u => u.UserId == userId)
                                                              .Include(u => u.Presentation)
                                                              .Select(u => u.Presentation);

            foreach (var word in keywords)
            {
                var lowerWord = word.ToLower();

                IQueryable<Presentation> presentationsSearch = userPresentations;

                if ((searchType & PresentationSearchType.Name) > 0)
                {
                    presentationsSearch = presentationsSearch.Where(p => p.Name.ToLower().Contains(lowerWord));
                }
                
                if ((searchType & PresentationSearchType.Description) > 0 )
                {
                    presentationsSearch = presentationsSearch.Where(p => p.Description.ToLower().Contains(lowerWord));
                }

                if ((searchType & PresentationSearchType.Tags) > 0 )
                {
                    presentationsSearch = presentationsSearch.Include(p => p.PresentationTags).
                        Where(p => p.PresentationTags.Any(pt => pt.Tag.Name.ToLower().Contains(lowerWord)));
                }

                var presentations = await presentationsSearch.ToListAsync();

                foreach (var item in presentations)
                {
                    if (!presentationsResult.Any(p => p.Id == item.Id))
                    {
                        presentationsResult.Add(item);
                    }
                }
            }

            var numOfPages = presentationsResult.Count / options.ItemsPerPage;

            return new PagedOperationResult<List<Presentation>>
            {
                Value = presentationsResult.Skip((options.PageIndex - 1) * options.ItemsPerPage).Take(options.ItemsPerPage).ToList(),
                TotalPages = numOfPages
            };
        }

        public async Task<PagedOperationResult<List<Presentation>>> PublicPresentations(PagingOptions options, string excludeUserIdIfAny)
        {
            var count = _context.Presentations.Count(p => p.IsPublic);
            List<Presentation> presentations;
            if (excludeUserIdIfAny != null)
            {
                presentations = await _context.Presentations.Where(p => p.IsPublic)
                    .Include(p => p.UserPresentations)
                    .Where(p => !p.UserPresentations.Any(up => up.UserId == excludeUserIdIfAny)).ToListAsync();
            }
            else
            {
                presentations = await _context.Presentations.Where(p => p.IsPublic).Skip(options.ToSkip).Take(options.ItemsPerPage)
                    .ToListAsync();
            }


            return new PagedOperationResult<List<Presentation>>
            {
                Value = presentations,
                TotalPages = count / options.ItemsPerPage,
                ItemsPerPage = options.ItemsPerPage
            };
        }

        public async Task<OperationStatus> AddPresentationToUser(int presentationId, string userId)
        {
            var alreadyExistent = _context.UserPresentations.Any(p => p.PresentationId == presentationId &&
                                                                 p.UserId == userId);

            if (! _context.Presentations.Any(p => p.Id == presentationId))
            {
                return new OperationStatus { ErrorMessageIfAny = "There is no presentation with the provided id" };
            }

            if (alreadyExistent)
            {
                return new OperationStatus { ErrorMessageIfAny = "The presentation already belongs to the user"};
            }

            var up = new UserPresentation { UserId = userId, PresentationId = presentationId };

            _context.UserPresentations.Add(up);
            var rows = await _context.SaveChangesAsync();
            if (rows == 0)
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = "An error ocurred while updating the database"
                };
            }
            return new OperationStatus();
        }
    }

}
