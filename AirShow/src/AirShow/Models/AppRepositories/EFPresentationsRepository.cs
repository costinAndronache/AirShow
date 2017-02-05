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

                    return await _filesRepository.DeleteFileWithId(presentation.FileID);
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
            var totalPages = (int)(Math.Ceiling((float)count / options.ItemsPerPage));

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

            return await _filesRepository.GetFileForId(upList.First().Presentation.FileID, inStream);
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
            currentPresentation.FileID = saveResult.Value;

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
                return new OperationStatus();
            }

            return new OperationStatus() { ErrorMessageIfAny = OperationStatus.kUnknownError };
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromCategory(string categoryName, string userId, PagingOptions options)
        {
            var count = _context.UserPresentations.Where(up => up.UserId == userId)
                        .Include(up => up.Presentation)
                        .Select(up => up.Presentation)
                        .Include(p => p.Category)
                        .Where(p => p.Category.Name == categoryName).Count();

            var upList = await _context.UserPresentations.Where(up => up.UserId == userId)
                .Include(up => up.Presentation)
                .Select(up => up.Presentation)
                .Include(p => p.Category)
                .Where(p => p.Category.Name == categoryName)
                .Skip(options.ToSkip)
                .Take(options.ItemsPerPage)
                .ToListAsync();


            var pages = Math.Ceiling((float)count / options.ItemsPerPage);

            var result =  new PagedOperationResult<List<Presentation>>
            {
                Value = upList,
                ItemsPerPage = options.ItemsPerPage,
                TotalPages = (int)pages
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
                TotalPages = (int)( Math.Ceiling((float)count / options.ItemsPerPage))
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
            var userPresentations = _context.UserPresentations.Where(u => u.UserId == userId)
                                                              .Include(u => u.Presentation)
                                                              .Select(u => u.Presentation);

            return await SearchInPresentations(userPresentations, keywords, options, searchType);
        }

        public async Task<PagedOperationResult<List<Presentation>>> SearchPublicPresentations(List<string> keywords, PagingOptions options,
                                                               PresentationSearchType searchType, string excludeFromUserId)
        {

            IQueryable<Presentation> selectedPresentations;
            if (excludeFromUserId != null)
            {
                selectedPresentations = _context.UserPresentations.Where(u => u.UserId != excludeFromUserId)
                    .Include(u => u.Presentation).Where(u => u.Presentation.IsPublic).Select(u => u.Presentation);
            } else
            {
                selectedPresentations = _context.Presentations.Where(p => p.IsPublic);
            }

            return await SearchInPresentations(selectedPresentations, keywords, options, searchType);
        }

        private async Task<PagedOperationResult<List<Presentation>>> SearchInPresentations(IQueryable<Presentation> selectedPresentations, 
            List<string> keywords, PagingOptions options, PresentationSearchType searchType)
        {
            var presentationsResult = new List<Presentation>();
            IQueryable<Presentation> finalPresentations = null;

            foreach (var word in keywords)
            {
                var lowerWord = word.ToLower();

                List<IQueryable<Presentation>> unionList = new List<IQueryable<Presentation>>();
                

                if ((searchType & PresentationSearchType.Name) > 0)
                {
                    var query = selectedPresentations.Where(p => p.Name.ToLower().Contains(lowerWord));
                    unionList.Add(query);
                }

                if ((searchType & PresentationSearchType.Description) > 0)
                {
                    var query = selectedPresentations.Where(p => p.Description.ToLower().Contains(lowerWord));
                    unionList.Add(query);
                }

                if ((searchType & PresentationSearchType.Tags) > 0)
                {
                    var query = selectedPresentations.Include(p => p.PresentationTags).
                        Where(p => p.PresentationTags.Any(pt => pt.Tag.Name.ToLower().Contains(lowerWord)));
                    unionList.Add(query); 
                }

                if (unionList.Count > 0)
                {
                    var endResult = unionList.First();
                    if (unionList.Count > 1)
                    {
                        for (int i = 1; i < unionList.Count; i++)
                        {
                            var queryableAtI = unionList[i];
                            endResult = endResult.Union(queryableAtI);
                        }
                    }
                    if (finalPresentations == null)
                    {
                        finalPresentations = endResult;
                    }else
                    {
                        finalPresentations = finalPresentations.Union(endResult);
                    }
                }
            }

            var numOfPages = (int)Math.Ceiling((float)finalPresentations.Count() / options.ItemsPerPage);
            if (numOfPages == 0) { numOfPages++; }
            return new PagedOperationResult<List<Presentation>>
            {
                Value = await finalPresentations.Skip(options.ToSkip).Take(options.ItemsPerPage).ToListAsync(),
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
                TotalPages = (int)Math.Ceiling((float)count / options.ItemsPerPage),
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


        public async Task<PagedOperationResult<List<Presentation>>> PublicPresentationsForUser(string userId, PagingOptions options, string excludeUserId)
        {
            var result = new PagedOperationResult<List<Presentation>>();

            var upList = _context.UserPresentations.Where(u => u.UserId == userId).Include(u => u.Presentation)
                .Where(u => u.Presentation.IsPublic);

            if (excludeUserId != null)
            {
                upList = upList.Where(u => !_context.UserPresentations.Any(up => up.UserId == excludeUserId && up.PresentationId == u.PresentationId));
            }

            var count = upList.Count();



            var list = await upList.Select(up => up.Presentation).
                Skip(options.ToSkip).Take(options.ItemsPerPage).
                ToListAsync();


            result.Value = list;
            result.TotalPages = count / options.ItemsPerPage;
            if (result.TotalPages == 0){result.TotalPages++;}
            return result;
        }

        public async Task<PagedOperationResult<List<Presentation>>> PublicPresentationsFromCategory(string categoryName, 
            PagingOptions options, string excludeUserId)
        {
            var result = new PagedOperationResult<List<Presentation>>();
            var categoryIdResult = await GetIdOfCategoryWithName(categoryName);
            if (categoryIdResult.ErrorMessageIfAny != null)
            {
                result.ErrorMessageIfAny = categoryIdResult.ErrorMessageIfAny;
                return result;
            }
            var catId = categoryIdResult.Value;

            var presentationSource = _context.Presentations.Where(p => p.CategoryId == catId && p.IsPublic);

            if (excludeUserId != null)
            {
                presentationSource = presentationSource.Where(p => !_context.UserPresentations.Any(u => u.UserId == excludeUserId
                && u.PresentationId == p.Id));
            }

            var count = presentationSource.Count();
            var presentations = await presentationSource
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

        public async Task<OperationResult<List<Presentation>>> GetPresentationsWithIds(List<int> idList)
        {
            var result = new OperationResult<List<Presentation>>();

            var presentations = await _context.Presentations.Where(p => idList.Contains(p.Id)).ToListAsync();
            result.Value = presentations;
            if (presentations == null)
            {
                result.Value = new List<Presentation>();
            }

            return result;
        }

        public async Task<OperationResult<Presentation>> GetPresentationForUser(string userId, string presentationName)
        {
            var result = new OperationResult<Presentation>();
            var upList = await _context.UserPresentations.Where(up => up.UserId == userId).Include(u => u.Presentation)
                .Where(up => up.Presentation.Name == presentationName).ToListAsync();

            if (upList.Count == 1)
            {
                result.Value = upList.First().Presentation;
            } else
            {
                result.ErrorMessageIfAny = "Could not find presentation for user";
            }
            return result;
        }


        public async Task<OperationStatus> UpdatePresentationForUser(string userId, string presentationName, UploadPresentationModel updateModel)
        {
            var opStatus = new OperationStatus();

            var foundPresentationResult = await GetPresentationForUser(userId, presentationName);
            if (foundPresentationResult.ErrorMessageIfAny != null)
            {
                return foundPresentationResult;
            }

            var presentation = foundPresentationResult.Value;
            if (!await IsUpdateNecessary(presentation, updateModel))
            {
                opStatus.ErrorMessageIfAny = "No modification required";
                return opStatus;
            }

            if (! NoOtherPresentationWithSameName(userId, presentation, updateModel))
            {
                opStatus.ErrorMessageIfAny = "Another presentation has the same name.";
                return opStatus;
            }

            if (!_context.Categories.Any(c => c.Id == updateModel.CategoryId))
            {
                return new OperationStatus { ErrorMessageIfAny = OperationStatus.kNoSuchCategoryWithId };
            }

            if (!presentation.IsPublic || _context.UserPresentations.Count(up => up.PresentationId == presentation.Id) == 1)
            {
                return await ModifyPresentationInPlace(userId, presentation, updateModel);
            }

            return await ModifyPresentationByCopying(userId, presentation, updateModel);
        }



        private async Task<OperationStatus> ModifyPresentationInPlace(string userId, Presentation p, UploadPresentationModel updateModel)
        {

            p.Name = updateModel.Name;
            p.Description = updateModel.Description;
            p.CategoryId = updateModel.CategoryId;
            p.IsPublic = updateModel.IsPublic;
            p.PresentationTags = new List<PresentationTag>();

            var associatedPTs = await _context.PresentationTags.Where(pt => pt.PresentationId == p.Id).ToListAsync();
            _context.PresentationTags.RemoveRange(associatedPTs);

            var newTagsResult = await _tagsRepository.CreateOrGetTags(updateModel.Tags);
            if (newTagsResult.ErrorMessageIfAny != null)
            {
                return newTagsResult;
            }

            foreach (var tag in newTagsResult.Value)
            {
                var pt = new PresentationTag
                {
                    Tag = tag,
                    Presentation = p
                };

                tag.PresentationTags.Add(pt);
                p.PresentationTags.Add(pt);

            }

            if (updateModel.SourceStream != null)
            {
                await _filesRepository.DeleteFileWithId(p.FileID);
                var createResult = await _filesRepository.SaveFile(updateModel.SourceStream);
                if (createResult.ErrorMessageIfAny != null)
                {
                    return createResult;
                }

                p.FileID = createResult.Value;
            }

            _context.Presentations.Update(p);
            var rows = await _context.SaveChangesAsync();
            if (rows == 0)
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = "An error ocurred while trying to update the database"
                };
            }

            return new OperationStatus();
        }

        private async Task<OperationStatus> ModifyPresentationByCopying(string userId, Presentation p, UploadPresentationModel updateModel)
        {
            var newPresentation = new Presentation
            {
                CategoryId = p.CategoryId,
                Name = p.Name,
                Description = p.Description,
                UploadedDate = p.UploadedDate,
                IsPublic = p.IsPublic,
                FileID = p.FileID,
                PresentationTags = new List<PresentationTag>()
            };

            var list = await _context.UserPresentations.Where(u => u.UserId == userId && u.PresentationId == p.Id).ToListAsync();
            _context.UserPresentations.Remove(list.First());

            _context.Presentations.Add(newPresentation);
            var rows = await _context.SaveChangesAsync();
            if (rows == 0)
            {
                return new OperationStatus
                {
                    ErrorMessageIfAny = "Error while trying to update the database"
                };
            }

            var newUP = new UserPresentation
            {
                UserId = userId,
                PresentationId = newPresentation.Id
            };

            _context.UserPresentations.Add(newUP);

            return await ModifyPresentationInPlace(userId, newPresentation, updateModel);
        }

        public async Task<bool> UserOwnsPresentation(string userId, int presentationId)
        {
            return _context.UserPresentations.Any(u => u.UserId == userId && u.PresentationId == presentationId);
        }


        private bool NoOtherPresentationWithSameName(string userId, Presentation p, UploadPresentationModel updateModel)
        {
            var numOfSameNamePresentations = _context.UserPresentations.Where(u => u.UserId == userId && u.PresentationId != p.Id)
                .Include(u => u.Presentation).Count(u => u.Presentation.Name == updateModel.Name);

            return numOfSameNamePresentations == 0;
        }

        private async Task<bool> IsUpdateNecessary(Presentation p, UploadPresentationModel updateModel)
        {
            if (updateModel.SourceStream != null)
            {
                return true;
            }

            if (updateModel.Name != p.Name || updateModel.Description != p.Description || updateModel.IsPublic != p.IsPublic)
            {
                return true;
            }

            if (updateModel.Tags != null)
            {
                var pTags = await _context.PresentationTags.Where(pt => pt.PresentationId == p.Id).Include(pt => pt.Tag)
                    .Select(pt => pt.Tag.Name).ToListAsync();

                if (pTags.Count() != updateModel.Tags.Count())
                {
                    return true;
                }

                if (updateModel.Tags.Any(t => !pTags.Contains(t)))
                {
                    return true;
                }
            }

            return false;
        }
    }

}
