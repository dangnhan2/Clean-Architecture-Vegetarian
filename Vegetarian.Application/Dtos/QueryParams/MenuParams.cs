using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.QueryParams
{
    public sealed class MenuParams : BaseQueryParams
    {
        [JsonProperty("from")]
        public decimal? From { get; set; }

        [JsonProperty("to")]
        public decimal? To { get; set; }

        [JsonProperty("sortBy")]
        public string? SortBy { get; set; }

        [JsonProperty("sortOrder")]
        public string? SortOrder { get; set; }
    }
}
