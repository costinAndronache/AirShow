using AirShow.Models.EF;
using AirShow.Views.Shared.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.ViewModels
{
    public class PresentationsViewModel: BaseViewModel
    {
        public List<PresentationCardModel> Presentations { get; set; }
        public PaginationViewModel PaginationModel { get; set; }
        public ButtonsToolbarModel ButtonsToolbarModel { get; set; }
        public string TopMessage { get; set; }
        public string TopMessageHref { get; set; }
        public string Title { get; set; }
        public LeftNavbar.IndexPair NavbarIndexPair { get; set; }
    }
}
