using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AirShow.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using AirShow.Models.EF;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using AirShow.Models.ViewModels;
using AirShow.WebSockets;
using Newtonsoft.Json;
using AirShow.Utils;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AirShow.Controllers
{
    [Authorize]
    public class PresentationsController : Controller
    {
        private UserManager<User> _userManager;
        private IPresentationsRepository _presentationsRepository;

        public PresentationsController(UserManager<User> userManager, 
                                        IPresentationsRepository presentationsRepository
                                       )
        {
            _presentationsRepository = presentationsRepository;
            _userManager = userManager;

        }
        // GET: /<controller>/
       

        public async Task<IActionResult> DownloadPresentation(string name)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var userId = _userManager.GetUserId(User);
                var result = await _presentationsRepository.DownloadPresentation(name, userId, stream);
                if (result.ErrorMessageIfAny != null)
                {
                    return NotFound();
                }

                FileContentResult fileResult = new FileContentResult(stream.ToArray(), "application/pdf");
                fileResult.FileDownloadName = name + ".pdf";
                return fileResult;
            };
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePresentation(string name)
        {
            var userID = _userManager.GetUserId(User);
            var result = await _presentationsRepository.DeletePresentation(name, userID);
            if (result.ErrorMessageIfAny != null)
            {
                return StatusCode(302);
            }

            return StatusCode(200);
        }
    }
}
