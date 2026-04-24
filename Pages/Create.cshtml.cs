using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskManager.Data;


namespace TaskManager.Pages
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TaskItem Task { get; set; } = new TaskItem();

        public void OnGet()
        {
            // Inicialización si hace falta
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Task.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(Task);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
