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

namespace AirShow.Models.AppRepositories
{
    public class EFRepository : IAppRepository
    {
        private EFCategoriesRepository _categoriesRepository;
        private EFPresentationsRepository _presentationsRepository;
        private EFTagsRepository _tagsRepository;
        private ThumbnailRepository _thumbnailRepository;

        public EFRepository(AirShowContext context,
                                IPresentationFilesRepository filesRepository)
        {
            _categoriesRepository = new EFCategoriesRepository(context);
            _tagsRepository = new EFTagsRepository(context);
            _thumbnailRepository = new ThumbnailRepository();

            _presentationsRepository = new EFPresentationsRepository(context, filesRepository, _tagsRepository, _thumbnailRepository);
               
        }

        public async Task<OperationStatus> AddTagsForPresentation(List<string> tags, Presentation p)
        {
            return await _tagsRepository.AddTagsForPresentation(tags, p);
        }

        public async Task<OperationResult<List<Tag>>> CreateOrGetTags(List<string> tagsAsStrings)
        {
            return await _tagsRepository.CreateOrGetTags(tagsAsStrings);
        }

        public async Task<OperationStatus> DeletePresentation(string presentationName, string userId)
        {
            return await _presentationsRepository.DeletePresentation(presentationName, userId);
        }

        public async Task<OperationStatus> DownloadPresentation(string name, string userId, Stream inStream)
        {
            return await _presentationsRepository.DownloadPresentation(name, userId, inStream);
        }

        public async Task<OperationResult<List<Category>>> GetCurrentCategories()
        {
            return await _categoriesRepository.GetCurrentCategories();
        }

        public async Task<OperationResult<int>> GetNumberOfPresentationsForUser(string userId)
        {
            return await _presentationsRepository.GetNumberOfPresentationsForUser(userId);
        }

        public async Task<OperationResult<int>> GetNumberOfUserPresentationsInCategory(string categoryName, string userId)
        {
            return await _presentationsRepository.GetNumberOfUserPresentationsInCategory(categoryName, userId);
        }

        public async Task<OperationResult<int>> GetNumberOfUserPresentationsWithTag(string tag, string userId)
        {
            return await _presentationsRepository.GetNumberOfUserPresentationsWithTag(tag, userId);
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetPresentationsForUser(string userId, PagingOptions options)
        {
            return await _presentationsRepository.GetPresentationsForUser(userId, options);
        }

        public async Task<OperationResult<List<Tag>>> GetTagsForPresentation(Presentation p)
        {
            return await _tagsRepository.GetTagsForPresentation(p);
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromCategory(string categoryName, string userId, PagingOptions options)
        {
            return await _presentationsRepository.GetUserPresentationsFromCategory(categoryName, userId, options);
        }

        public async Task<PagedOperationResult<List<Presentation>>> GetUserPresentationsFromTag(string tag, string userId, PagingOptions options)
        {
            return await _presentationsRepository.GetUserPresentationsFromTag(tag, userId, options);
        }

        public async Task<OperationStatus> RemoveTagFromPresentation(string tag, Presentation p)
        {
            return await _tagsRepository.RemoveTagFromPresentation(tag, p);
        }

        public async Task<OperationStatus> UploadPresentationForUser(string userId, UploadPresentationModel model)
        {
            return await _presentationsRepository.UploadPresentationForUser(userId, model);
        }

        public async Task<PagedOperationResult<List<Presentation>>> SearchUserPresentations(List<string> keywords, string userId, PagingOptions options,
                                                                       PresentationSearchType searchType)
        {
            return await _presentationsRepository.SearchUserPresentations(keywords, userId, options, searchType);
        }

        public async Task<OperationStatus> AddThumbnailFor(Presentation p, Stream fileStream)
        {
            return await _thumbnailRepository.AddThumbnailFor(p, fileStream);
        }

        public async Task<OperationResult<string>> GetThumbnailURLFor(Presentation p)
        {
            return await _thumbnailRepository.GetThumbnailURLFor(p);
        }

        public async Task<OperationResult<Category>> GetCategoryForPresentation(Presentation p)
        {
            return await _categoriesRepository.GetCategoryForPresentation(p);
        }
    }
}
