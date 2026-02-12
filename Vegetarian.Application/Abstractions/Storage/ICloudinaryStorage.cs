using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Abstractions.Storage
{
    public interface ICloudinaryStorage
    {
        public Task DeleteImage(string url);
        public Task<string> UploadImage(IFormFile file, string folder);
    }
}
