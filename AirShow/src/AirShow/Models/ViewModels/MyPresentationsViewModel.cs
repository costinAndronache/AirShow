using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class PresentationsViewModel: BaseViewModel
    {
        public List<MyPresentationCardModel> Presentations { get; set; }
        public PaginationViewModel PaginationModel { get; set; }
    }
}
