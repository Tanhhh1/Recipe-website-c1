using EProject.NETCore.Filter;
using EProject.NETCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EProject.NETCore.Controllers
{
    public class SubmissionController : Controller
    {
        [HttpGet]
        public IActionResult UploadSubmission(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewData["CompetitionId"] = id;
            // Generate a random string
            string guid = Guid.NewGuid().ToString();
            ViewData["Guid"] = guid;
            return View("upload_submission");
        }
 
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
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/submission", tempFolderName);
                Directory.CreateDirectory(imageDirectory);

                fileName = Path.GetFileNameWithoutExtension(uploadedFiles.FileName);
                extension = Path.GetExtension(uploadedFiles.FileName);
                imageName = fileName + DateTime.Now.ToString("yyyyMMddHHmmss");
                imageSavePath = Path.Combine(imageDirectory, imageName + extension);

                using (var stream = new FileStream(imageSavePath, FileMode.Create))
                {
                    await uploadedFiles.CopyToAsync(stream);
                }

                returnImagePath = "/postedImage/submission/" + tempFolderName + "/" + imageName + extension;
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
       
        [HttpPost]
        public IActionResult DeleteTempImages([FromBody] DeleteImagesRequest request)
        {
            try
            {
                // Delete the folder containing temporary images
                string guid = request.Guid;
                string imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "submission", guid);

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
        [HttpPost]
        public async Task<IActionResult> UploadSubmission(Submission submission, string guid)
        {
          
            ModelState.Remove("Competition");
           
            using (var db = new JamesThewContext())
            {
                var competition = await db.Competitions.FirstOrDefaultAsync(c => c.Id == submission.CompetitionId);

                if (competition == null)
                {

                    // If competition is not found, return an error
                    ModelState.AddModelError("Fullname", "Competition not found.");

                }
                else
                {
                    if (competition.EndDate < DateTime.UtcNow)
                    {
                        // If expired, return error
                        ModelState.AddModelError("Fullname", "The competition has ended.");
                    }
                }
            }
            // If there are errors in ModelState, return the current page with errors
            if (!ModelState.IsValid)
            {
                ViewData["CompetitionId"] = submission.CompetitionId;
                ViewData["Guid"] = guid;
                ViewData["Fullname"] = submission.Fullname;
                ViewData["Email"] = submission.Email;
                ViewData["Title"] = submission.Title;
                ViewData["Content"] = submission.Content;
                return View("upload_submission");
            }
            using (var db = new JamesThewContext())
            {
                // Add submission to database
                submission.CreatedDate = DateTime.Now;
                db.Submissions.Add(submission);
                db.SaveChanges();
                // Create a folder containing submission images based on id
                string newFolderName = submission.Id.ToString();
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/submission", newFolderName);
                Directory.CreateDirectory(imageDirectory);



                // Extract image path from post content
                List<string> imageUrls = ExtractImageUrlsFromHtml(submission.Content);
                // Move images from temporary folder to new folder
                string tempImageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/submission", guid);
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
                            submission.Content =submission.Content.Replace($"/postedImage/submission/{guid}/{imageName}", $"/postedImage/submission/{newFolderName}/{imageName}");

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
            TempData["Notification"] = "Submission uploaded successfully";
            return RedirectToAction("DetailContest", "Contest", new { id = submission.CompetitionId });

        }
        [HttpGet("detailSubmission/{SubId}")]
        public async Task<IActionResult> detailSubmission(int? SubId)
        {
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Variable to track if the user is an admin.
                var isAdmin = false;
                // Retrieve the logged-in user's ID from the session.
                var userId = HttpContext.Session.GetString("UserID");
                // Check if the user is logged in.
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        isAdmin = user.Role;
                    }
                }
                // If the user is not an admin, show access denied view.
                if (!isAdmin)
                {
                    return View("~/Views/Recipe/accessdenied.cshtml");
                }
                // Find the submission by ID.
                var submission = await db.Submissions.FirstOrDefaultAsync(m => m.Id == SubId);
                // If the submission is not found, return NotFound result.
                if (submission == null)
                {
                    return NotFound();
                }
                return View(submission);
            }
        }
        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> IsWinner(int id)
        {
            using (var db = new JamesThewContext())
            {
                // Check admin to display the interface only admin has
                var userId = HttpContext.Session.GetString("UserID");
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user.Role == false)
                    {
                        return NotFound();
                    }
                }
                var winningSubmission = db.Submissions.FirstOrDefault(s => s.IsWinner == true);
                if (winningSubmission != null)
                {
                    winningSubmission.IsWinner = false;
                    db.Submissions.Update(winningSubmission);
                }
                // Find submission by id
                var submission = await db.Submissions.FindAsync(id);

                if (submission == null)
                {
                    return NotFound();
                }

                // Update the IsWinner property to true
                submission.IsWinner = true;

                // Check if an announcement already exists for this competition
                var announcement = await db.Announcements
                    .FirstOrDefaultAsync(a => a.CompetitionId == submission.CompetitionId);

                if (announcement == null)
                {

                    // If does not exist, create a new Announcement
                    announcement = new Announcement
                    {
                        CompetitionId = submission.CompetitionId,
                        Date = DateTime.Now
                    };
                    db.Announcements.Add(announcement);
                }
                else
                {

                    // If already exists, update date
                    announcement.Date = DateTime.Now;
                    db.Announcements.Update(announcement);
                }


                // Save changes to the database
                await db.SaveChangesAsync();
                TempData["Notification"] = "The winner has been successfully chosen!";
                return RedirectToAction("DetailContest", "Contest", new { id = submission.CompetitionId });
            }
        }
    }

}
