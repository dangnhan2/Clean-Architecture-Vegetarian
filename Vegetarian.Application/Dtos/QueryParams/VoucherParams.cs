using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.QueryParams
{
    public sealed class VoucherParams : BaseQueryParams
    {
        [JsonPropertyName("from")]
        public DateTime? From { get; set; }

        [JsonPropertyName("to")]
        public DateTime? To { get; set; }
    }
}
