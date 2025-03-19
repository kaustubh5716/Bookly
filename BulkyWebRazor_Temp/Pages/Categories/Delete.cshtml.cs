using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    [BindProperties]
    public class DeleteModel : PageModel
    {

        public Category CategoryFromDb { get; set; }
        private readonly ApplicationDbContext _db;

        public DeleteModel(ApplicationDbContext db)
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
            _db.Categories.Remove(CategoryFromDb);
            _db.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
