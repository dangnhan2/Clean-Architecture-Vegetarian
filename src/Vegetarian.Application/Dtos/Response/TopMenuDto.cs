using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class TopMenuDto
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int SoldQuantity { get; set; }
    }
}
