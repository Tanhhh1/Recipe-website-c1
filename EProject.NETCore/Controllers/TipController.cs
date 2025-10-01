using EProject.NETCore.Filter;
using EProject.NETCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using X.PagedList;

namespace EProject.NETCore.Controllers
{
    public class TipController : Controller
    {
        [HttpGet("tip")]
        public async Task<IActionResult> Tip(int? page, int? pageSize)
        {
            // Check if page or pageSize is not provided, set default values
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 8;
            }
            // Use context to retrieve the list of recipes with pagination
            using (var db = new JamesThewContext())
            {
                var query = db.Guidances
                              .Include(g => g.User)
                              .Where(g => g.Type == true) // Filter recipes with type = true (type = 1)
                              .OrderByDescending(b => b.Id); // Sort recipes by Id in descending order

                var list = await query
                                 .Skip((page.Value - 1) * pageSize.Value) // Skip recipes before the current page
                                 .Take(pageSize.Value) // Take the number of recipes according to pageSize
                                 .ToListAsync(); // Convert to list asynchronously

                var totalCount = await query.CountAsync(); // Count the total number of filtered records
                var pagedList = new StaticPagedList<Guidance>(list, page.Value, pageSize.Value, totalCount); // Create static paged list

                return View(pagedList);
            }
        }
        [HttpGet("tip/search")]
        public async Task<IActionResult> Search(string input_search, string input_free, string input_premium, int? page, int? pageSize)
        {
            // Check if page or pageSize is not provided, set default values
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 8;
            }
            // Use context to retrieve the list of guidances with pagination and search criteria
            using (var db = new JamesThewContext())
            {
                // Base query filtered by type = 0
                var query = db.Guidances
                              .Include(g => g.User)
                              .Where(g => g.Type == true) // Filter by type = 1 (tips)
                              .AsQueryable();
                // If input_search is not empty, add search condition by Title
                if (!string.IsNullOrEmpty(input_search))
                {
                    query = query.Where(g => g.Title.Contains(input_search));
                }
                // Add search condition by is_free (Free, Premium)
                if (input_free == "1" && input_premium == "0")
                {
                    // Filter for Free types only
                    query = query.Where(g => g.IsFree == true || g.IsFree == false); // Free type is true
                }
                else if (input_free == "1")
                {
                    // Filter for Free types only
                    query = query.Where(g => g.IsFree == true); // Free type is true
                }
                else if (input_premium == "0")
                {
                    // Filter for Premium types only
                    query = query.Where(g => g.IsFree == false); // Premium type is false
                }
                // Retrieve the list of guidances with pagination and applied conditions
                var list = await query
                                 .OrderByDescending(g => g.Id)
                                 .Skip((page.Value - 1) * pageSize.Value)
                                 .Take(pageSize.Value)
                                 .ToListAsync();
                var totalCount = await query.CountAsync();
                var pagedList = new StaticPagedList<Guidance>(list, page.Value, pageSize.Value, totalCount);
                // Store value in ViewBag
                ViewBag.InputSearch = input_search;
                ViewBag.InputFree = input_free;
                ViewBag.InputPremium = input_premium;
                return View("Tip", pagedList); // Ensure that the view is named "Recipe"
            }
        }
        [HttpGet]
        [SessionCheck]
        public async Task<IActionResult> UploadTip()
        {
            // Check admin to display the interface only admin has
            var isAdmin = false;
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using (var db = new JamesThewContext())
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        isAdmin = user.Role;
                    }
                }
            }
            ViewData["isAdmin"] = isAdmin;
            // Generate a random string
            string guid = Guid.NewGuid().ToString();
            ViewData["Guid"] = guid;
            return View("upload_tip");
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
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/tip", tempFolderName);
                Directory.CreateDirectory(imageDirectory);

                fileName = Path.GetFileNameWithoutExtension(uploadedFiles.FileName);
                extension = Path.GetExtension(uploadedFiles.FileName);
                imageName = fileName + DateTime.Now.ToString("yyyyMMddHHmmss");
                imageSavePath = Path.Combine(imageDirectory, imageName + extension);

                using (var stream = new FileStream(imageSavePath, FileMode.Create))
                {
                    await uploadedFiles.CopyToAsync(stream);
                }

                returnImagePath = "/postedImage/tip/" + tempFolderName + "/" + imageName + extension;
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
                string imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "tip", guid);

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
        public async Task<IActionResult> UploadTip(Guidance tip, string guid)
        {


            ModelState.Remove("User");
            // Check admin to display the interface only admin has
            var isAdmin = false;
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using (var db = new JamesThewContext())
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        isAdmin = user.Role;
                    }
                }
            }
            // If there are errors in ModelState, return the current page with errors
            if (!ModelState.IsValid)
            {

                ViewData["isAdmin"] = isAdmin;
                ViewData["Guid"] = guid;
                ViewData["Title"] = tip.Title;
                ViewData["Content"] = tip.Content;
                ViewData["Img"] = tip.Img;
                return View("upload_tip");
            }
            using (var db = new JamesThewContext())
            {
                // Add tip to database
                tip.Type = true;
                if (isAdmin == false)
                {
                    tip.IsFree = false;
                }
                tip.UserId = int.Parse(HttpContext.Session.GetString("UserID"));
                tip.CreatedDate = DateTime.Now;
                db.Guidances.Add(tip);
                db.SaveChanges();
                // Create a folder containing tip images based on id
                string newFolderName = tip.Id.ToString();
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/tip", newFolderName);
                Directory.CreateDirectory(imageDirectory);



                // Extract image path from post content
                List<string> imageUrls = ExtractImageUrlsFromHtml(tip.Content);
                imageUrls.Add(tip.Img);
                // Move images from temporary folder to new folder
                string tempImageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/tip", guid);
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
                            tip.Content = tip.Content.Replace($"/postedImage/tip/{guid}/{imageName}", $"/postedImage/tip/{newFolderName}/{imageName}");

                        }
                        else
                        {
                            // If not in the list, delete the temporary file
                            System.IO.File.Delete(tempImage);
                        }
                    }
                    // Update the recipe's image path
                    string img = Path.GetFileName(tip.Img); ;
                    tip.Img = tip.Img.Replace($"/postedImage/tip/{guid}/{img}", $"/postedImage/tip/{newFolderName}/{img}");
                    // Delete temporary directory
                    Directory.Delete(tempImageDirectory, true);
                }

                // Save changes to the database
                await db.SaveChangesAsync();

            }
            TempData["Notification"] = "Tip added successfully";
            return RedirectToAction("Tip");
        }

        [HttpGet]
        public async Task<IActionResult> TipDetail(int? id)
        {

            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the tip by ID.
                var tip = await db.Guidances.FirstOrDefaultAsync(m => m.Id == id && m.Type == true);
                // Return not found if the tip does not exist.
                if (tip == null)
                {
                    return NotFound();
                }
                if (tip.IsFree == false)
                {
                    if (HttpContext.Session.GetString("UserID") == null)
                    {
                        return RedirectToAction("AccessDenied");
                    }
                }
                // Pass the tip data to the view using ViewBag.
                ViewBag.Tip = tip;
                return View("tip_detail");
            }
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [SessionCheck]
        [HttpGet]
        public async Task<IActionResult> UpdateTip(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }
            // Check admin to display the interface only admin has
            var isAdmin = false;
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using (var db = new JamesThewContext())
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        isAdmin = user.Role;
                    }
                }
            }
            ViewData["isAdmin"] = isAdmin;
            // Generate random string
            string guid = Guid.NewGuid().ToString();
            ViewData["Guid"] = guid;
            using (var db = new JamesThewContext())
            {
                var tip = await db.Guidances.FirstOrDefaultAsync(m => m.Id == id && m.Type == true);
                if (tip == null)
                {
                    return NotFound();
                }
                if (tip.UserId.ToString() != HttpContext.Session.GetString("UserID"))
                {
                    return RedirectToAction("AccessDenied");
                }

                return View("update_tip", tip);
            }
        }
        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> UpdateTip(Guidance tipNew, string guid)
        {
            // Check admin 
            var isAdmin = false;
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using (var db = new JamesThewContext())
                {
                    var user = await db.Users.FindAsync(int.Parse(userId));
                    if (user != null)
                    {
                        isAdmin = user.Role;
                    }
                }
            }
            using (var db = new JamesThewContext())
            {
                var tip = await db.Guidances.FirstOrDefaultAsync(m => m.Id == tipNew.Id && m.Type == true);
                if (tip == null)
                {
                    return NotFound();
                }
                // Check to see if the owner of the tip is valid
                if (tip.UserId.ToString() != HttpContext.Session.GetString("UserID"))
                {
                    return RedirectToAction("AccessDenied");
                }
                ModelState.Remove("User");
                // If there are errors in ModelState, return the current page with errors
                if (!ModelState.IsValid)
                {
                    ViewData["isAdmin"] = isAdmin;
                    return View("update_tip", tip);
                }
                tip.Title = tipNew.Title;
                tip.Content = tipNew.Content;
                tip.Img = tipNew.Img;
                if (isAdmin == false)
                {
                    tip.IsFree = false;
                }
                else
                {
                    tip.IsFree = tipNew.IsFree;
                }
                tip.UpdatedDate = DateTime.Now;

                // Get the directory containing tip images based on id
                string newFolderName = tip.Id.ToString();
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/tip", newFolderName);


                // Extract image path from post content
                List<string> imageUrls = ExtractImageUrlsFromHtml(tip.Content);
                imageUrls.Add(tip.Img);
                // Move images from temporary folder to recipe images folder
                string tempImageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/tip", guid);
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
                            tip.Content = tip.Content.Replace($"/postedImage/tip/{guid}/{imageName}", $"/postedImage/tip/{newFolderName}/{imageName}");

                        }
                        else
                        {
                            // If not in the list, delete the temporary file
                            System.IO.File.Delete(tempImage);
                        }
                    }
                    // Update the tip's image path
                    string img = Path.GetFileName(tip.Img); ;
                    tip.Img = tip.Img.Replace($"/postedImage/tip/{guid}/{img}", $"/postedImage/tip/{newFolderName}/{img}");
                    // Delete temporary directory
                    Directory.Delete(tempImageDirectory, true);
                }
                // If the image is not in the post content, delete it
                if (Directory.Exists(imageDirectory))
                {
                    string[] existingImages = Directory.GetFiles(imageDirectory);
                    foreach (var existingImage in existingImages)
                    {
                        string imageName = Path.GetFileName(existingImage);
                        if (!imageUrls.Any(url => url.Contains(imageName)))
                        {
                            System.IO.File.Delete(existingImage);
                        }
                    }
                }

                await db.SaveChangesAsync();

            }
            TempData["Notification"] = "Tip updated successfully";
            return RedirectToAction("TipDetail", new { id = tipNew.Id });

        }
    }
}
