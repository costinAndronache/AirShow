using AirShow.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Interfaces
{
    public interface IMailService
    {
        Task<OperationStatus> SendMessageToAddress(string message, string address);
    }
}
