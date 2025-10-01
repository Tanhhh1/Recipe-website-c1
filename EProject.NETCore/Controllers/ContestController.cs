using EProject.NETCore.Filter;
using EProject.NETCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;
using X.PagedList;

namespace EProject.NETCore.Controllers
{
    public class ContestController : Controller
    {
        [HttpGet("contest")]
        public async Task<IActionResult> Contest(int? page, int? pageSize)
        {

            // Check if page or pageSize is not provided, set default values
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 5;
            }
            // Use context to retrieve the list of tips with pagination
            List<Competition> list;
            using (var db = new JamesThewContext())
            {
                list = await db.Competitions
                                .OrderByDescending(b => b.Id) // Sort tips by Id in descending order
                                .Skip((page.Value - 1) * pageSize.Value)  // Skip tips before the current page
                                .Take(pageSize.Value) // Take the number of tips according to pageSize
                                .ToListAsync(); // Convert to list asynchronously
                var totalCount = await db.Competitions.CountAsync();
                var pagedList = new StaticPagedList<Competition>(list, page.Value, pageSize.Value, totalCount); // Create static paged list


                var isAdmin = false;
                var userId = HttpContext.Session.GetString("UserID");
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        isAdmin = user.Role;
                    }
                }
                ViewData["isAdmin"] = isAdmin;
                return View(pagedList);
            }
        }
        [SessionCheck]
        [HttpGet]
        public async Task<IActionResult> NewContest()
        {
            // Check admin to display the interface only admin has
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using (var db = new JamesThewContext())
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user.Role == false)
                    {
                        return NotFound();
                    }
                }
            }
            // Generate a random string
            string guid = Guid.NewGuid().ToString();
            ViewData["Guid"] = guid;
            return View();
        }
        [SessionCheck]
        [HttpPost]
        public async Task<JsonResult> UploadFile(IFormFile uploadedFiles, string guid)
        {
            string returnImagePath = string.Empty;
            string fileName;
            string extension;
            string imageName;
            string imageSavePath;

            if (uploadedFiles != null && uploadedFiles.Length > 0)
            {
                // Create temporary directory according to GUID
                string tempFolderName = guid;
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/contest", tempFolderName);
                Directory.CreateDirectory(imageDirectory);

                fileName = Path.GetFileNameWithoutExtension(uploadedFiles.FileName);
                extension = Path.GetExtension(uploadedFiles.FileName);
                imageName = fileName + DateTime.Now.ToString("yyyyMMddHHmmss");
                imageSavePath = Path.Combine(imageDirectory, imageName + extension);

                using (var stream = new FileStream(imageSavePath, FileMode.Create))
                {
                    await uploadedFiles.CopyToAsync(stream);
                }

                returnImagePath = "/postedImage/contest/" + tempFolderName + "/" + imageName + extension;
            }

            return Json(new { path = returnImagePath });
        }
        public List<string> ExtractImageUrlsFromHtml(string htmlContent)
        {
            List<string> imageUrls = new List<string>();

            // Regex pattern to find image paths
            string pattern = @"<img.*?src=(['""])(.*?)\1.*?>";

            MatchCollection matches = Regex.Matches(htmlContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                string imageUrl = match.Groups[2].Value;
                imageUrls.Add(imageUrl);
            }

            return imageUrls;
        }
        [SessionCheck]
        [HttpPost]
        public IActionResult DeleteTempImages([FromBody] DeleteImagesRequest request)
        {
            try
            {
                // Delete the folder containing temporary images
                string guid = request.Guid;
                string imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "contest", guid);

                if (Directory.Exists(imageDirectoryPath))
                {
                    Directory.Delete(imageDirectoryPath, true);
                    return Ok(new { message = "Images deleted successfully." });
                }
                else
                {
                    return NotFound(new { message = "Image directory not found." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting images: {ex.Message}" });
            }
        }

        public class DeleteImagesRequest
        {
            public string Guid { get; set; }
        }
        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> NewContest(Competition competition, string guid)
        {
            // Check admin to display the interface only admin has
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using (var db = new JamesThewContext())
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user.Role == false)
                    {
                        return NotFound();
                    }
                }
            }
            if (competition.StartDate == default(DateTime))
            {
                // Handle the case where StartDate is not set
                ModelState.AddModelError("StartDate", "Start Date is required.");
            }

            if (competition.EndDate == default(DateTime))
            {
                // Handle the case where EndDate is not set
                ModelState.AddModelError("EndDate", "End Date is required.");
            }
            if (competition.StartDate != default(DateTime) && competition.EndDate != default(DateTime) && competition.EndDate < competition.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date cannot be earlier than Start Date.");
            }
            // If there are errors in ModelState, return the current page with errors
            if (!ModelState.IsValid)
            {
                ViewData["Guid"] = guid;
                ViewData["Description"] = competition.Description;
                return View("newContest");
            }
            using (var db = new JamesThewContext())
            {
                // Add contest to database
                db.Competitions.Add(competition);
                db.SaveChanges();
                // Create a folder containing contest images based on id
                string newFolderName = competition.Id.ToString();
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/contest", newFolderName);
                Directory.CreateDirectory(imageDirectory);



                // Extract image path from post content
                List<string> imageUrls = ExtractImageUrlsFromHtml(competition.Description);
                // Move images from temporary folder to new folder
                string tempImageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/contest", guid);
                if (Directory.Exists(tempImageDirectory))
                {
                    string[] tempImages = Directory.GetFiles(tempImageDirectory);
                    foreach (var tempImage in tempImages)
                    {
                        string imageName = Path.GetFileName(tempImage);
                        string newImagePath = Path.Combine(imageDirectory, imageName);

                        // Check if the image is in the imageUrls list
                        if (imageUrls.Any(url => url.Contains(imageName)))
                        {
                            // Move the image
                            System.IO.File.Move(tempImage, newImagePath);

                            // Update the path in the post content
                            competition.Description = competition.Description.Replace($"/postedImage/contest/{guid}/{imageName}", $"/postedImage/contest/{newFolderName}/{imageName}");

                        }
                        else
                        {
                            // If not in the list, delete the temporary file
                            System.IO.File.Delete(tempImage);
                        }
                    }

                    // Delete temporary directory
                    Directory.Delete(tempImageDirectory, true);
                }

                // Save changes to the database
                db.SaveChanges();
            }
            TempData["Notification"] = "Contest added successfully";
            return RedirectToAction("Contest");
        }
        [HttpGet]
        public async Task<IActionResult> DetailContest(int id, int? page, int? pageSize)
        {
            // Set default values for page and pageSize if not provided.
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 5;
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the competition by id.
                var competition = await db.Competitions
                                    .FirstOrDefaultAsync(c => c.Id == id);
                // If competition is not found, return NotFound.
                if (competition == null)
                {
                    return NotFound();
                }
                List<Submission> submissions = new List<Submission>();
                // Prepare a query to fetch submissions for the competition.
                var subQuery = db.Submissions.Where(s => s.CompetitionId == id);
                // Fetch the paginated list of submissions.
                submissions = await subQuery
                                    .OrderByDescending(s => s.CreatedDate)
                                    .Skip((page.Value - 1) * pageSize.Value)
                                    .Take(pageSize.Value)
                                    .ToListAsync();
                // Get the total count of submissions.
                var totalSub = await subQuery.CountAsync();
                // Create a StaticPagedList to handle pagination in the view.
                ViewBag.Submissions = new StaticPagedList<Submission>(submissions, page.Value, pageSize.Value, totalSub);
                // Pass the competition details to the view.
                ViewBag.Competition = competition;
                return View("DetailContest");
            }
        }
        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> DeleteContest(int id)
        {
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Get the current user's ID from the session.
                var userId = HttpContext.Session.GetString("UserID");
                // Fetch the user from the database.
                var user = await db.Users.FindAsync(int.Parse(userId));
                // Check if the user exists and if they have admin rights
                if (user == null || !user.Role)
                {
                    return View("~/Views/Recipe/accessdenied.cshtml");
                }
                // Fetch the contest with its related submissions and announcements.
                var contest = await db.Competitions
                                      .Include(c => c.Submissions)
                                      .Include(c => c.Announcements)
                                      .FirstOrDefaultAsync(c => c.Id == id);
                // If contest is not found, return NotFound.
                if (contest == null)
                {
                    return NotFound();
                }
                string imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "contest", id.ToString());
                if(Directory.Exists(imageDirectoryPath)) {
                    Directory.Delete(imageDirectoryPath, true);
                }
             
                // Remove related announcements and submissions.
                db.Announcements.RemoveRange(contest.Announcements);
                db.Submissions.RemoveRange(contest.Submissions);
                // Remove the contest itself.
                db.Competitions.Remove(contest);
                // Save changes to the database.

                await db.SaveChangesAsync();
                TempData["Notification"] = "Contest deleted successfully";
                return RedirectToAction("Contest");
            }
        }

    }
}
