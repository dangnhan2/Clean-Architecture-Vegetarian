using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Request
{
    public class SearchRequestDto
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; }
    }
}
