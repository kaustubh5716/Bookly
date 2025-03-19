using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _iWebHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork,IWebHostEnvironment WebHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _iWebHostEnvironment = WebHostEnvironment;
            
        }
        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.product.GetAll(includeProperties:"Category").ToList();
            return View("Index" ,objProductList);
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.category
                    .GetAll().Select(u => new SelectListItem
                    {
                        Text = u.Name,
                        Value = u.Id.ToString()
                    }),
                Product = new Product()

            };
            if (id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.product.Get(u=> u.Id == id);
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM,IFormFile? file)
        {
            ModelState.Remove("ISBN");
            ModelState.Remove("Price");
            ModelState.Remove("Title");
            ModelState.Remove("Author");
            ModelState.Remove("Price50");
            ModelState.Remove("Price100");
            ModelState.Remove("ListPrice");
            ModelState.Remove("Description");
            ModelState.Remove("CategoryList");
            if (ModelState.IsValid)
            {
                string wwwRootPath = _iWebHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\Product");

                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        //delete old path 
                        var oldPath =
                            Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    } 

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productVM.Product.ImageUrl = @"\images\Product\" + fileName;
                }

                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.product.Update(productVM.Product);
                }
                    _unitOfWork.save();
                TempData["Success"] = "Product Created Successfully";
                return RedirectToAction("Index", "Product");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.category
                    .GetAll().Select(u => new SelectListItem
                    {
                        Text = u.Name,
                        Value = u.Id.ToString()
                    });
                return View(productVM);
            }
        }
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product ProductToBeDeleted = _unitOfWork.product.Get(u => u.Id == id);
            if (ProductToBeDeleted == null)
            {
                return Json(new {success= false, message = "Error while Deleting" });
            }
            string wwwRootPath = _iWebHostEnvironment.WebRootPath;
                //delete old path 
                var oldPath =
                    Path.Combine(wwwRootPath, ProductToBeDeleted.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                _unitOfWork.product.Remove(ProductToBeDeleted);
                _unitOfWork.save();
                TempData["Success"] = "Product Delete Successfully";
                return Json(new { success = true, message = "Delete Successfull" }); ;
        }


        #endregion

    }
}
