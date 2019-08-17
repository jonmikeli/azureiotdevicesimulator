﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IoT.Simulator2.Services
{
    public interface IMessageService
    {
        Task<string> GetMessageAsync();

        Task<string> GetMessageAsync(string deviceId, string moduleId);

        Task<string> GetRandomizedMessageAsync(string deviceId, string moduleId);
    }
}
