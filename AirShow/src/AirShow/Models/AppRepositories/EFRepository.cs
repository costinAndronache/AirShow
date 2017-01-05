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

        public EFRepository(AirShowContext context,
                                IPresentationFilesRepository filesRepository)
        {
            _categoriesRepository = new EFCategoriesRepository(context);
            _tagsRepository = new EFTagsRepository(context);
            _presentationsRepository = new EFPresentationsRepository(context, filesRepository, _tagsRepository);    
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

        public async Task<OperationStatus> UploadPresentationForUser(string name, string description, string userId, int categoryId, List<string> tags, Stream stream)
        {
            return await _presentationsRepository.UploadPresentationForUser(name, description, userId, categoryId, tags, stream);
        }
    }
}
