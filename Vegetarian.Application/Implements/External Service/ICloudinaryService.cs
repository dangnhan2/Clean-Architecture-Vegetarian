using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Implements.External_Service
{
    public interface ICloudinaryService
    {
        public Task DeleteImage(string url);
        public Task<string> UploadImage(IFormFile file, string folder);
    }
}
