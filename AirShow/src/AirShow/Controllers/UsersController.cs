using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.Models.Interfaces;
using AirShow.Models.Common;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    public class UsersController : Controller
    {
        private IUsersRepository _usersRepository;

        public UsersController(IUsersRepository usersRepository)
        {
            _usersRepository = usersRepository;
        }


        public async Task<IActionResult> GetUsersForPresentation(int presentationId, int? page, int? itemsPerPage)
        {
            var pagingOptions = PagingOptions.CreateWithTheseOrDefaults(page, itemsPerPage);
            var usersResult = await _usersRepository.GetUsersForPresentation(presentationId, pagingOptions);
            if (usersResult.ErrorMessageIfAny != null)
            {
                return new JsonResult(new { errorMessage = usersResult.ErrorMessageIfAny });
            }
            var usersList = usersResult.Value.Select(u => new { id = u.Id, name = u.Name });

            return new JsonResult(usersList);
        }

    }
}
