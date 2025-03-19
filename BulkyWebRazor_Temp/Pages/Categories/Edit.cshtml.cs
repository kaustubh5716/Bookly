using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public Category Category { get; set; }

        public Category CategoryFromDb { get; set; }

        public EditModel(ApplicationDbContext db)
        {
            _db = db;

        }
        public void OnGet(int? id)
        {
            if (id != null)
            {

                CategoryFromDb = _db.Categories.Find(id);
            }
        }

        public IActionResult OnPost()
        {
            _db.Categories.Update(CategoryFromDb);
            _db.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
