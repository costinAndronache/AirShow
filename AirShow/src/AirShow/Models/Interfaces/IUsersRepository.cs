using AirShow.Models.Common;
using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Interfaces
{
    public interface IUsersRepository
    {
        Task<PagedOperationResult<List<User>>> GetUsersForPresentation(int presentationId, PagingOptions options);

    }
}
