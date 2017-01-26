using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Common
{
    public class OperationStatus
    {
        //TO DO: Must put these in config jsons
        public const string kPresentationWithSameNameExists = "A presentation with the same name already exists in your account!";
        public const string kNoSuchCategoryWithId = "No such category with that id";
        public const string kUnknownError = "Unknown error";
        public const string kInvalidFileNameOrAlreadyExists = "A file with the same name already exists or the filename contains invalid characters";

        public string ErrorMessageIfAny { get; set; }
    }
}
