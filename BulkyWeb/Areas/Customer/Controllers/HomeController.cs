using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models;
using Bulky.Models.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Product> productList = _unitOfWork.product.GetAll(includeProperties: "Category");
        return View(productList);
    }
    [Authorize]

    public IActionResult Details(int id)
    {

        ShoppingCart Cart = new ShoppingCart();
        Cart.Count = 1;
        Cart.ProductId = id;
        //most most most important partttt
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        Cart.ApplicationUserId = userId;
        Product product = _unitOfWork.product.Get(u => u.Id == id, includeProperties: "Category");
        Cart.Product = product; 
        return View(Cart);
    }
    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart obj)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        if (ModelState.IsValid)
        {
            obj.Id = 0; // Ensure the database generates the ID automatically
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u =>
                u.ApplicationUserId == obj.ApplicationUserId && u.ProductId == obj.ProductId);
            if(cartFromDb != null)
            {
                //same item with same user exist 
                cartFromDb.Count += obj.Count; 
                _unitOfWork.shoppingCart.Update(cartFromDb);
                _unitOfWork.save();

            }
            else
            {
                _unitOfWork.shoppingCart.Add(obj);
                _unitOfWork.save();
                HttpContext.Session.SetInt32(SD.sessionCart ,_unitOfWork.shoppingCart.GetAll(u => 
                    u.ApplicationUserId == userId).Count());

            }
           
            TempData["Success"] = "Product Add to Cart Successfully";
        }

        return RedirectToAction("Index");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
