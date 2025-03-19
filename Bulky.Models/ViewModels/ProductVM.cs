using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

using Bulky.Models.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models.ViewModels
{
    public class ProductVM
    {
        
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        public Product Product { get; set; }

    }
}
