using EProject.NETCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Drawing.Printing;
using X.PagedList;

namespace EProjet.NETCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult HomePage()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Announcements(int? page, int? pageSize)
        {
            // Set default values for page and pageSize if they are not provided.
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 4;
            }
            // Create a new database context.
            using (var context = new JamesThewContext())
            {
                // Prepare a query to fetch announcements with related competition data.
                var query = context.Announcements
                    .Include(a => a.Competition)
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        a.Date,
                        CompetitionTitle = a.Competition.Title,
                        StartDate = a.Competition.StartDate,
                        EndDate = a.Competition.EndDate,
                        WinnerFullname = context.Submissions
                                               .Where(s => s.CompetitionId == a.CompetitionId && s.IsWinner == true)
                                               .Select(s => s.Fullname)
                                               .FirstOrDefault()
                    });
                // Get the total count of announcements.
                var totalCount = await query.CountAsync();
                // Fetch the paginated list of announcements.
                var list = await query
                                 .Skip((page.Value - 1) * pageSize.Value)
                                 .Take(pageSize.Value)
                                 .ToListAsync();
                // Create a StaticPagedList to handle pagination in the view.
                var pagedList = new StaticPagedList<dynamic>(list, page.Value, pageSize.Value, totalCount);

                return View(pagedList);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult FAQ()
        {
            return View();
        }
       
    }
}
