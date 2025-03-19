using Bulky.Models;
using Bulky.DataAccess;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    { 
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Category> objCategoryList = _unitOfWork.category.GetAll().ToList();//Retrive seeds from data 
            return View(objCategoryList);//pass the obj
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name" , "The Display order cannot exactly match with Category name.");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.category.Add(obj);
                _unitOfWork.save();
                TempData["Success"] = "Category Created Successfully";
                return RedirectToAction("Index", "Category");
            }

            return View();


        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category categoryFromDb = _unitOfWork.category.Get(u=>u.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }
        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.category.Update(obj);
                _unitOfWork.save();
                TempData["Success"] = "Category Updated Successfully";
                return RedirectToAction("Index", "Category");
            }

            return View();


        }
        [BindProperty]
        public Category categoryFromDb { get; set; }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

             categoryFromDb = _unitOfWork.category.Get(u => u.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(Category categoryFromDb)
        {
            _unitOfWork.category.Remove(categoryFromDb);
            _unitOfWork.save();

                TempData["Success"] = "Category Deleted Successfully";
                return RedirectToAction("Index", "Category");
            
        }

    }
}
