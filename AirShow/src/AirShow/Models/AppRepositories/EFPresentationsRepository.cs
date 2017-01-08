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

        public async Task<OperationStatus> UploadPresentationForUser(string userId, UploadPresentationModel model)
        {
            if (_context.Presentations.Any(p => p.UserId == userId && p.Name == model.Name))
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
                UserId = userId,
                CategoryId = model.CategoryId,
                PresentationTags = new List<PresentationTag>(),
                UploadedDate = DateTime.Now,
                IsPublic = model.IsPublic
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
                var saveResult =  await _filesRepository.SaveFileForUser(model.SourceStream, model.Name, userId);
                if (saveResult.ErrorMessageIfAny != null)
                {
                    return saveResult;
                }

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

            var list = await _context.Presentations.Include(pt => pt.Category).Where(p => p.Category.Name == categoryName && p.UserId == userId)
                .Skip((options.PageIndex - 1) * options.ItemsPerPage).Take(options.ItemsPerPage).ToListAsync();

            return new PagedOperationResult<List<Presentation>>
            {
                Value = list,
                ItemsPerPage = options.ItemsPerPage
            };
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromTag(string tag, string userId, PagingOptions options)
        {
            var listOfAllPresentations = await _context.Presentations.Include(p => p.PresentationTags)
                .Where(p => p.PresentationTags.Any(pt => pt.Tag.Name == tag) && p.UserId == userId).ToListAsync();

            var filteredList = listOfAllPresentations
                .Skip((options.PageIndex - 1) * options.ItemsPerPage).Take(options.ItemsPerPage);

            return new PagedOperationResult<List<Presentation>>
            {
                Value = filteredList.ToList(),
                ItemsPerPage = options.ItemsPerPage
            };
        }

        public async Task<OperationResult<int>> GetNumberOfUserPresentationsInCategory(string categoryName, string userId)
        {
            var numOfItems = _context.Presentations.Include(p => p.Category).Count(pt => pt.UserId == userId && pt.Category.Name == categoryName);
            var result = new OperationResult<int>
            {
                Value = numOfItems
            };

            return result;
        }

        public async Task<OperationResult<int>> GetNumberOfUserPresentationsWithTag(string tag, string userId)
        {
            var numOfItems = _context.Presentations.Include(p => p.PresentationTags).Count(
                p => p.PresentationTags.Any(pt => pt.Tag.Name == tag) && p.UserId == userId);

            return new OperationResult<int>
            {
                Value = numOfItems
            };
        }

        public async Task<PagedOperationResult<List<Presentation>>> SearchUserPresentations(List<string> keywords, string userId, PagingOptions options,
                                                                       PresentationSearchType searchType)
        {
            var presentationsResult = new List<Presentation>();
            var userPresentations = _context.Presentations.Where(p => p.UserId == userId);

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

    }

}
