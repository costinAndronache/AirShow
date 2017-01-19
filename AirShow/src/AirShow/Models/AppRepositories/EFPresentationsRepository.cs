﻿using System;
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
            var singledOutUP = await upList.Where(up => _context.Presentations.Any(p => p.Id == up.PresentationId && p.Name == presentationName))
                .Include(up => up.Presentation).ToListAsync();

            if (singledOutUP.Count == 1)
            {
                var up = singledOutUP.First();
                var presentation = up.Presentation;
                _context.UserPresentations.Remove(up);
                var rows = await _context.SaveChangesAsync();

                if (rows <= 0)
                {
                    return new OperationStatus
                    {
                        ErrorMessageIfAny = "Error while trying to update the database."
                    };
                }

                if (! await _context.UserPresentations.AnyAsync(u => u.PresentationId == presentation.Id))
                {
                    var presentationTags = _context.PresentationTags.Where(pt => pt.PresentationId == presentation.Id).ToList();
                    _context.Presentations.Remove(presentation);
                    _context.PresentationTags.RemoveRange(presentationTags);
                    _context.SaveChanges();

                    return await _filesRepository.DeleteFileWithId(presentation.FileId);
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
            var totalPages = count / (options.ItemsPerPage > 0 ? options.ItemsPerPage : 1);

            var upList = await _context.UserPresentations.Where(up => up.UserId == userId).Include(up => up.Presentation)
                .Select(up => up.Presentation).Skip(toSkip).Take(toTake).ToListAsync();


            var result =  new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                TotalPages = totalPages,
                ItemsPerPage = options.ItemsPerPage
            };
            if (result.TotalPages == 0) { result.TotalPages++; }
            return result;
        }

        public async Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream)
        {
            var upList = await _context.UserPresentations.Where(up => up.UserId == userId).Include(up => up.Presentation)
                .Where(up => up.Presentation.Name == name)
                .ToListAsync();

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

            
            var result =  new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                ItemsPerPage = options.ItemsPerPage
            };
            if (result.TotalPages == 0) { result.TotalPages++; }
            return result;
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromTag(string tag, string userId, PagingOptions options)
        {
            var count = _context.UserPresentations.Where(u => u.UserId == userId)
                .Include(u => u.Presentation)
                .Select(u => u.Presentation)
                .Include(p => p.PresentationTags)
                .Count(p => p.PresentationTags.Any(pt => pt.Tag.Name == tag));

            var upList = await _context.UserPresentations.Where(u => u.UserId == userId)
                .Include(u => u.Presentation)
                .Select(u => u.Presentation)
                .Include(p => p.PresentationTags)
                .Where(p => p.PresentationTags.Any(pt => pt.Tag.Name == tag))
                .Skip(options.ToSkip)
                .Take(options.ItemsPerPage)
                .ToListAsync();

            var result =  new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                ItemsPerPage = options.ItemsPerPage,
                TotalPages = count / options.ItemsPerPage
            };
            if (result.TotalPages == 0) { result.TotalPages++; }
            return result;
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
            if (numOfPages == 0){ numOfPages++; }
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


            var result =  new PagedOperationResult<List<Presentation>>
            {
                Value = presentations,
                TotalPages = count / options.ItemsPerPage,
                ItemsPerPage = options.ItemsPerPage
            };
            if (result.TotalPages == 0) { result.TotalPages++; }
            return result;
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


        public async Task<PagedOperationResult<List<Presentation>>> PublicPresentationsForUser(string userId, PagingOptions options)
        {
            var result = new PagedOperationResult<List<Presentation>>();

            var count = _context.UserPresentations.Where(up => up.UserId == userId).Include(up => up.Presentation)
                .Count(up => up.Presentation.IsPublic);

            var list = await _context.UserPresentations.Where(up => up.UserId == userId).Include(up => up.Presentation)
                .Where(up => up.Presentation.IsPublic).Select(up => up.Presentation).
                Skip(options.ToSkip).Take(options.ItemsPerPage).
                ToListAsync();


            result.Value = list;
            result.TotalPages = count / options.ItemsPerPage;
            if (result.TotalPages == 0){result.TotalPages++;}
            return result;
        }

        public async Task<PagedOperationResult<List<Presentation>>> PublicPresentationsFromCategory(string categoryName, PagingOptions options)
        {
            var result = new PagedOperationResult<List<Presentation>>();
            var categoryIdResult = await GetIdOfCategoryWithName(categoryName);
            if (categoryIdResult.ErrorMessageIfAny != null)
            {
                result.ErrorMessageIfAny = categoryIdResult.ErrorMessageIfAny;
                return result;
            }
            var catId = categoryIdResult.Value;
            var count = _context.Presentations.Count(p => p.CategoryId == catId && p.IsPublic);
            var presentations = await _context.Presentations.Where(p => p.CategoryId == catId && p.IsPublic)
                .Skip(options.ToSkip).Take(options.ItemsPerPage)
                .ToListAsync();

            result =  new PagedOperationResult<List<Presentation>>
            {
                Value = presentations,
                TotalPages = count / options.ItemsPerPage
            };
            if (result.TotalPages == 0) { result.TotalPages++; }
            return result;
        }
        public async Task<PagedOperationResult<List<Presentation>>> UserPresentationsFromCategory(string userId, string categoryName, PagingOptions options)
        {
            var result = new PagedOperationResult<List<Presentation>>();
            var categoryIdResult = await GetIdOfCategoryWithName(categoryName);
            if (categoryIdResult.ErrorMessageIfAny != null)
            {
                result.ErrorMessageIfAny = categoryIdResult.ErrorMessageIfAny;
                return result;
            }
            var catId = categoryIdResult.Value;

            var userPresentations = await _context.UserPresentations.Where(u => u.UserId == userId).
                Include(u => u.Presentation).Where(u => u.Presentation.CategoryId == catId)
                .Select(u => u.Presentation)
                .Skip(options.ToSkip).Take(options.ItemsPerPage)
                .ToListAsync();


            var count = _context.UserPresentations.Where(u => u.UserId == userId).Include(u => u.Presentation).Count(u =>
            u.Presentation.CategoryId == catId);

            result.Value = userPresentations;
            result.TotalPages = count / options.ItemsPerPage;
            if (result.TotalPages == 0){result.TotalPages++;}
            return result;
        }


        private async Task<OperationResult<int>> GetIdOfCategoryWithName(string categoryName)
        {
            var result = new OperationResult<int>();
            var categoriesList = await _context.Categories.Where(c => c.Name == categoryName).ToListAsync();
            if (categoriesList.Count != 1)
            {
                result.ErrorMessageIfAny = $"No category with name {categoryName} found.";
                return result;
            }
            var category = categoriesList.First();
            result.Value = category.Id;
            return result;
        }

    }

}
