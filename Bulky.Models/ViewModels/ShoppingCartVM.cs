using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.Models.Models;

namespace Bulky.Models.ViewModels
{
    public class ShoppingCartVM
    {
        public IEnumerable<ShoppingCart> ShoppingList { get; set; }
        public OrderHeader OrderHeader { get; set; } 
    }
}
