using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirShow.Models.Interfaces;
using AirShow.Models.Common;
using AirShow.Models.EF;
using AirShow.Models.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AirShow.Models.AppRepositories
{
    public class EFUsersRepository: IUsersRepository
    {
        private AirShowContext _context;

        public EFUsersRepository(AirShowContext context)
        {
            _context = context;
        }

        public async Task<PagedOperationResult<List<User>>> GetUsersForPresentation(int presentationId, PagingOptions options)
        {
            var result = new PagedOperationResult<List<User>>();
            var presentationList = await _context.Presentations.Where(p => p.Id == presentationId).ToListAsync();
            if (presentationList.Count == 0)
            {
                result.ErrorMessageIfAny = "No such presentation found";
                return result;
            }

            if (presentationList.First().IsPublic == false)
            {
                result.ErrorMessageIfAny = "The presentation cannot be accessed because it is private";
                return result;
            }

            var count = _context.UserPresentations.Count(up => up.PresentationId == presentationId);
            var upList = await _context.UserPresentations.Where(up => up.PresentationId == presentationId).Include(up => up.User).Select(up => up.User)
                .Skip(options.ToSkip).Take(options.ItemsPerPage).ToListAsync();

            result.TotalPages = count / options.ItemsPerPage;
            result.Value = upList;

            return result;
        }
    }
}
