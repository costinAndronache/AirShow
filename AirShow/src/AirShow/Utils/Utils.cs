using AirShow.Models.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Common
{
    public class AirshowUtils
    {
        public static string ControllerName(Type controllerType)
        {
            return nameof(controllerType).Replace("Controller", "");
        }

        public static OperationStatus ConfirmDirectoryExistsOrCreate(string directoryPath)
        {

            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    var di = Directory.CreateDirectory(directoryPath);
                }
                catch (Exception e)
                {
                    
                    return new OperationStatus
                    {
                        ErrorMessageIfAny = "Unknown error"
                    };
                }
            }

            return new OperationStatus();
        }

        public static FileStream CreateFileToWriteAtPath(string path)
        {
            try
            {
                FileStream fs = new FileStream(path, FileMode.CreateNew);
                return fs;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
