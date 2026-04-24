using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;

namespace TaskManager.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public IndexModel(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IList<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public string? CurrentUser { get; set; }

        public async Task OnGetAsync()
        {
            // Cargar tareas
            Tasks = await _context.Tasks
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Consultar identidad SQL
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand("SELECT SUSER_SNAME();", connection);
            var result = command.ExecuteScalar();
            CurrentUser = result?.ToString();
        }
    }
}
