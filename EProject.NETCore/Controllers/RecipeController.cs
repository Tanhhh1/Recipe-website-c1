using Microsoft.AspNetCore.Mvc;
using EProject.NETCore.Models;
using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using EProject.NETCore.Filter;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Data;
namespace EProjet.NETCore.Controllers
{
    public class RecipeController : Controller
    {
        [HttpGet("recipe")]
        public async Task<IActionResult> Recipe(int? page, int? pageSize)
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
                              .Where(g => g.Type == false) // Filter recipes with type = false (type = 0)
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
        [HttpGet("recipe/search")]
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
                              .Where(g => g.Type == false) // Filter by type = 0 (recipes)
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
                return View("Recipe", pagedList); // Ensure that the view is named "Recipe"
            }
        }



        [HttpGet]
        [SessionCheck]
        public async Task<IActionResult> UploadRecipe()
        {
            // Check admin to display the interface only admin has
            var isAdmin = false;
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                using(var db = new JamesThewContext())
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
            return View("upload_recipe");
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
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/recipe", tempFolderName);
                Directory.CreateDirectory(imageDirectory);

                fileName = Path.GetFileNameWithoutExtension(uploadedFiles.FileName);
                extension = Path.GetExtension(uploadedFiles.FileName);
                imageName = fileName + DateTime.Now.ToString("yyyyMMddHHmmss");
                imageSavePath = Path.Combine(imageDirectory, imageName + extension);

                using (var stream = new FileStream(imageSavePath, FileMode.Create))
                {
                    await uploadedFiles.CopyToAsync(stream);
                }

                returnImagePath = "/postedImage/recipe/" + tempFolderName + "/" + imageName + extension;
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
                string imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "recipe", guid);

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
        public async Task<IActionResult> UploadRecipe(Guidance recipe, string guid)
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
                ViewData["Title"] = recipe.Title;
                ViewData["Content"] = recipe.Content;
                ViewData["Img"] = recipe.Img;
                return View("upload_recipe");
            }
            using (var db = new JamesThewContext())
            {
                // Add recipe to database
                recipe.Type = false;
                if(isAdmin == false)
                {
                    recipe.IsFree = false;
                }
                recipe.UserId = int.Parse(HttpContext.Session.GetString("UserID"));
                recipe.CreatedDate = DateTime.Now;
                db.Guidances.Add(recipe);
                db.SaveChanges();

                // Create a folder containing recipe images based on id
                string newFolderName = recipe.Id.ToString();
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/recipe", newFolderName);
                Directory.CreateDirectory(imageDirectory);


                // Extract image path from post content
                List<string> imageUrls = ExtractImageUrlsFromHtml(recipe.Content);
                imageUrls.Add(recipe.Img);

                // Move images from temporary folder to new folder
                string tempImageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/recipe", guid);
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
                            recipe.Content = recipe.Content.Replace($"/postedImage/recipe/{guid}/{imageName}", $"/postedImage/recipe/{newFolderName}/{imageName}");
                           
                        }
                        else
                        {
                            // If not in the list, delete the temporary file
                            System.IO.File.Delete(tempImage);
                        }
                    }
                    // Update the recipe's image path
                    string img = Path.GetFileName(recipe.Img); 
                    recipe.Img = recipe.Img.Replace($"/postedImage/recipe/{guid}/{img}", $"/postedImage/recipe/{newFolderName}/{img}");

                    // Delete temporary directory
                    Directory.Delete(tempImageDirectory, true);
                }

                // Save changes to the database
                await db.SaveChangesAsync();

            }
            TempData["Notification"] = "Recipe added successfully";
            return RedirectToAction("Recipe");
        }
        [HttpGet]
        public async Task<IActionResult> RecipeDetail(int? id, int? page, int? pageSize)
        {
            // Set default values for page and pageSize if not provided.
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 4;
            }
            // If id is null, return NotFound result.
            if (id == null)
            {
                return NotFound();
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the recipe by ID.
                var recipe = await db.Guidances.FirstOrDefaultAsync(m => m.Id == id && m.Type == false);
                // Return NotFound if the recipe does not exist.
                if (recipe == null)
                {
                    return NotFound();
                }
                if (recipe.IsFree == false)
                {
                    if(HttpContext.Session.GetString("UserID") == null) {
                        return RedirectToAction("AccessDenied");
                    }
                }
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
                // Initialize an empty list of feedback.
                List<Feedback> feedback = new List<Feedback>();
                // If the user is an admin, fetch and paginate feedback for the recipe.
                if (isAdmin)
                {
                    var feedbackQuery = db.Feedbacks.Where(f => f.GuidanceId == id);
                    feedback = await feedbackQuery
                                        .OrderByDescending(g => g.Id)
                                        .Skip((page.Value - 1) * pageSize.Value)
                                        .Take(pageSize.Value)
                                        .ToListAsync();

                    var totalFeedbacks = await feedbackQuery.CountAsync();
                    ViewBag.Feedbacks = new StaticPagedList<Feedback>(feedback, page.Value, pageSize.Value, totalFeedbacks);
                }
                else
                {
                    ViewBag.Feedbacks = null;
                }

                ViewBag.Recipe = recipe;// Pass recipe data to the view using ViewBag.
                ViewBag.IsAdmin = isAdmin;// Pass admin status to the view using ViewBag.
                return View("recipe_detail");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecipeDetail(int id, string fullname, string email, string content, int? page = 1, int? pageSize = 4)
        {
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the recipe by ID.
                var recipe = await db.Guidances.FirstOrDefaultAsync(m => m.Id == id && m.Type == false);
                // Return NotFound if the recipe does not exist.
                if (recipe == null)
                {
                    return NotFound();
                }

                // Validate the form inputs
                if (string.IsNullOrWhiteSpace(fullname))
                {
                    ModelState.AddModelError("fullname", "Full name is required.");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    ModelState.AddModelError("email", "Email is required.");
                }
                else if (!new EmailAddressAttribute().IsValid(email))
                {
                    ModelState.AddModelError("email", "The email address is not in a valid format.");
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    ModelState.AddModelError("content", "Content is required.");
                }
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

                // If validation fails, reload the page with current recipe details and feedbacks
                if (!ModelState.IsValid)
                {
                    List<Feedback> feedbackList = new List<Feedback>();
                    if (isAdmin)
                    {
                        var feedbackQuery = db.Feedbacks.Where(f => f.GuidanceId == id);

                        // Paginate the feedback list
                        feedbackList = await feedbackQuery
                            .OrderByDescending(f => f.Id)
                            .Skip((page.Value - 1) * pageSize.Value)
                            .Take(pageSize.Value)
                            .ToListAsync();

                        var totalFeedbacks = await feedbackQuery.CountAsync();
                        ViewBag.Feedbacks = new StaticPagedList<Feedback>(feedbackList, page.Value, pageSize.Value, totalFeedbacks);
                    }
                    else
                    {
                        ViewBag.Feedbacks = null;
                    }

                    ViewBag.Recipe = recipe; // Pass recipe data to the view using ViewBag.
                    ViewBag.IsAdmin = isAdmin; // Pass admin status to the view using ViewBag.

                    return View("recipe_detail");
                }

                // Add new feedback if validation passes
                var newFeedback = new Feedback
                {
                    Fullname = fullname,
                    Email = email,
                    GuidanceId = id,
                    Content = content,
                    CreatedDate = DateTime.Now
                };
                db.Feedbacks.Add(newFeedback);
                await db.SaveChangesAsync();
                TempData["Notification"] = "Feedback for Recipe sended successfully";
                // Redirect to the GET method to ensure feedbacks are properly displayed
                return RedirectToAction("RecipeDetail", new { id = id });
            }
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [SessionCheck]
        [HttpGet]
        public async Task<IActionResult> UpdateRecipe(int? id)
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
                var recipe = await db.Guidances.FirstOrDefaultAsync(m => m.Id == id && m.Type == false);
                if (recipe == null)
                {
                    return NotFound();
                }
                // Check to see if the owner of the recipe is valid
                if (recipe.UserId.ToString() != HttpContext.Session.GetString("UserID"))
                {
                    return RedirectToAction("AccessDenied");
                }

                return View("update_recipe", recipe);
            }
        }
        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> UpdateRecipe(Guidance recipeNew, string guid)
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
            using (var db = new JamesThewContext())
            {
                var recipe = await db.Guidances.FirstOrDefaultAsync(m => m.Id == recipeNew.Id && m.Type == false);

                if (recipe == null)
                {
                    return NotFound();
                }

                // Check to see if the owner of the recipe is valid
                if (recipe.UserId.ToString() != HttpContext.Session.GetString("UserID"))
                {
                    return RedirectToAction("AccessDenied");
                }
                ModelState.Remove("User");
                // If there are errors in ModelState, return the current page with errors
                if (!ModelState.IsValid)
                {
                    ViewData["isAdmin"] = isAdmin;
                    return View("update_recipe", recipe);
                }
                recipe.Title = recipeNew.Title;
                recipe.Content = recipeNew.Content;
                recipe.Img = recipeNew.Img;
                if(isAdmin == false)
                {
                    recipe.IsFree = false;
                }
                else
                {
                    recipe.IsFree = recipeNew.IsFree;
                }
                recipe.UpdatedDate= DateTime.Now;

                // Get the directory containing recipe images based on id
                string newFolderName = recipe.Id.ToString();
                string imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/recipe", newFolderName);


                // Extract image path from post content
                List<string> imageUrls = ExtractImageUrlsFromHtml(recipe.Content);
                imageUrls.Add(recipe.Img);
                // Move images from temporary folder to recipe images folder
                string tempImageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/postedImage/recipe", guid);
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
                            recipe.Content = recipe.Content.Replace($"/postedImage/recipe/{guid}/{imageName}", $"/postedImage/recipe/{newFolderName}/{imageName}");

                        }
                        else
                        {
                            // If not in the list, delete the temporary file
                            System.IO.File.Delete(tempImage);
                        }
                    }
                    // Update the recipe's image path
                    string img = Path.GetFileName(recipe.Img); ;
                    recipe.Img = recipe.Img.Replace($"/postedImage/recipe/{guid}/{img}", $"/postedImage/recipe/{newFolderName}/{img}");
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
            TempData["Notification"] = "Recipe updated successfully";
            return RedirectToAction("RecipeDetail", new { id = recipeNew.Id });

        }
    }
}

