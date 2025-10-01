using EProject.NETCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using X.PagedList;
using EProject.NETCore.Filter;

namespace EProject.NETCore.Controllers
{
    public class UserController : Controller
    {
        private readonly StripeSettings _stripeSettings;

        public UserController(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings.Value;
        }

        public ActionResult Register()
        {
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Homepage", "Home");
            }
            return View();
        }
        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            using (var db = new JamesThewContext())
            {
                return !await db.Users.AnyAsync(u => u.Username == username);
            }
        }

        public async Task<IActionResult> CreateCheckoutSession(User user)
        {
            // Check if the username already exists
            if (!await IsUsernameUniqueAsync(user.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
            }
            // If there are errors in ModelState, return the current page with errors
            if (!ModelState.IsValid)
            {
                TempData["Username"] = user.Username;
                TempData["Email"] = user.Email;
                TempData["Fullname"] = user.Fullname;
                return View("register");
            }
            // Make payments using stripe
            int price = 0;
            string membership = "";
            switch (user.MembershipType)
            {
                case 1:
                    membership = "Monthly";
                    break;
                case 2:
                    membership = "Yearly";
                    break;
                default:
                    break;
            }


            switch (membership)
            {
                case "Monthly":
                    price = 10;
                    break;
                case "Yearly":
                    price = 100;
                    break;
                default:
                    break;
            }
            var currency = "usd";
            var successUrl = Url.Action("Success", "User", null, Request.Scheme);
            var cancelUrl = Url.Action("Register", "User", null, Request.Scheme);
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
            {
                "card"
            },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = price * 100,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = membership + " Subscription",
                            Description = "You will have " + membership.ToLower() + " access to all the paid recipes and tips from James Thew. Additionally, you can also upload recipes and tips as well as manage what you post."
                        }
                    },
                    Quantity = 1
                }
            },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            var session = service.Create(options);
            // Save data temporarily
            TempData["Username"] = user.Username;
            TempData["Password"] = user.Password;
            TempData["Email"] = user.Email;
            TempData["Fullname"] = user.Fullname;
            TempData["MembershipType"] = user.MembershipType.ToString();
            TempData["ExpirationDate"] = DateTime.Now.ToString();

            return Redirect(session.Url);
        }
        public IActionResult Success()
        {
            // Get data from TempData
            var username = TempData["Username"] as string;
            var password = PasswordHelper.HashPassword(TempData["Password"] as string);
            var email = TempData["Email"] as string;
            var fullname = TempData["Fullname"] as string;
            var membershipTypeStr = TempData["MembershipType"] as string;
            var expirationDateStr = TempData["ExpirationDate"] as string;


            // Exchange data
            if (int.TryParse(membershipTypeStr, out int membershipType) &&
                DateTime.TryParse(expirationDateStr, out DateTime expirationDate))
            {
                using (var db = new JamesThewContext())
                {

                    // Add User object to DbSet and save to database
                    var user = new User
                    {
                        Username = username,
                        Password = password,
                        Email = email,
                        Fullname = fullname,
                        MembershipType = (byte?)membershipType,
                        ExpirationDate = expirationDate,
                        Role = false
                    };

                    db.Users.Add(user);
                    db.SaveChanges();
                }
            }

            return View();
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            var userId = HttpContext.Session.GetString("UserID");
            if (!string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Homepage", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            ModelState.Remove("ReturnUrl");
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = model.ReturnUrl;
                return View(model);
            }
            // check login
            using (var db = new JamesThewContext())
            {
                var user = await db.Users
               .Where(u => u.Username == model.Username)
               .FirstOrDefaultAsync();

                if (user != null && PasswordHelper.VerifyPassword(model.Password, user.Password))
                {

                    // Save user information into session
                    HttpContext.Session.SetString("UserID", user.Id.ToString());
                    HttpContext.Session.SetString("FullName", user.Fullname);
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl); // Redirect to URL before logging in
                    }
                    else
                    {
                        return Redirect("/");
                    }
                }
                
                else
                {
                    ModelState.AddModelError("Username", "Invalid login attempt.");
                    return View(model);
                }
            }
        }

        [SessionCheck]
        [HttpGet]
        public async Task<IActionResult> UserInformation(int? id)
        {
            // Check if the ID is null, or if the logged-in user ID does not match the provided ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (id == null || loggedInUserId == null || id.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            if (id == null)
            {
                return NotFound();
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var user = await db.Users.FirstOrDefaultAsync(m => m.Id == id);
                // Return not found if user does not exist.
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
        }
        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> UserInformation(int id, string fullname, string username)
        {
            // Check if the logged-in user ID does not match the provided ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (id == null || loggedInUserId == null || id.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var user = await db.Users.FindAsync(id);
                // Validate the input fields.
                if (user == null)
                {
                    return NotFound();
                }
                if (string.IsNullOrWhiteSpace(fullname))
                {
                    ModelState.AddModelError("fullname", "Full name is required.");
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    ModelState.AddModelError("username", "Username is required.");
                }
                else if (db.Users.Any(u => u.Username == username && u.Id != id))
                {
                    ModelState.AddModelError("username", "Username is already taken.");
                }
                // If validation fails, return the view with the existing user data and validation errors.
                if (!ModelState.IsValid)
                {
                    return View(user);
                }
                // Update user information.
                user.Fullname = fullname;
                user.Username = username;
                HttpContext.Session.SetString("FullName", user.Fullname);
                db.Users.Update(user); // Update user in the database.

                await db.SaveChangesAsync(); // Save changes to the database.
                TempData["Notification"] = "Change Information successfully";

                return RedirectToAction("UserInformation", new { id = user.Id });
            }
        }

        [SessionCheck]
        [HttpGet("changePassword/{userId}")]
        public async Task<IActionResult> ChangePassword(int userId)
        {
            // Check if the logged-in user ID does not match the provided user ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (userId == null || loggedInUserId == null || userId.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var user = await db.Users.FindAsync(userId);
                // Return not found if user does not exist.
                if (user == null)
                {
                    return NotFound();
                }
                ViewData["UserId"] = userId; // Pass the user ID to the view.
                return View(user);
            }
        }

        [SessionCheck]
        [HttpPost("changePassword/{userId}")]
        public async Task<IActionResult> ChangePassword(int userId, string old_pass, string new_pass, string re_pass)
        {
            // Check if the logged-in user ID does not match the provided user ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (userId == null || loggedInUserId == null || userId.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            // Validate the input fields.
            if (string.IsNullOrWhiteSpace(old_pass))
            {
                ModelState.AddModelError("old_pass", "The old password is required.");
            }
            if (string.IsNullOrWhiteSpace(new_pass))
            {
                ModelState.AddModelError("new_pass", "The new password is required.");
            }
            else if (new_pass != re_pass)
            {
                ModelState.AddModelError("re_pass", "The new password and re-entered password do not match.");
            }
            // If validation fails, return the view with validation errors.
            if (!ModelState.IsValid)
            {
                return View();
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var user = await db.Users.FindAsync(userId);
                // Return not found if user does not exist.
                if (user == null)
                {
                    return NotFound();
                }
                // Verify the old password.
                if (!BCrypt.Net.BCrypt.Verify(old_pass, user.Password))
                {
                    ModelState.AddModelError("old_pass", "The old password is incorrect.");
                    return View();
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(new_pass); // Update the password.
                db.Users.Update(user); // Update the user in the database.
                await db.SaveChangesAsync(); // Save changes to the database.
                TempData["Notification"] = "Change Password successfully";

                return RedirectToAction("UserInformation", new { id = user.Id });
            }
        }

        [SessionCheck]
        [HttpGet("changeEmail/{userId}")]
        public async Task<IActionResult> ChangeEmail(int userId)
        {
            // Check if the logged-in user ID does not match the provided user ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (userId == null || loggedInUserId == null || userId.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var user = await db.Users.FindAsync(userId);
                // Return not found if user does not exist.
                if (user == null)
                {
                    return NotFound();
                }
                ViewData["UserId"] = userId; // Pass the user ID to the view.
                return View(user);
            }
        }

        [SessionCheck]
        [HttpPost("changeEmail/{userId}")]
        public async Task<IActionResult> ChangeEmail(int userId, string newEmail, string password)
        {
            // Check if the logged-in user ID does not match the provided user ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (userId == null || loggedInUserId == null || userId.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            // Validate the input fields.
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                ModelState.AddModelError("newEmail", "The new email address is required.");
            }
            else if (!new EmailAddressAttribute().IsValid(newEmail))
            {
                ModelState.AddModelError("newEmail", "The new email address is not in a valid format.");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("pass", "The password is required.");
            }
            // If validation fails, return the view with validation errors.
            if (!ModelState.IsValid)
            {
                return View(new User { Id = userId });
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var user = await db.Users.FindAsync(userId);
                // Return not found if user does not exist.
                if (user == null)
                {
                    return NotFound();
                }
                // Verify the password.
                if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    ModelState.AddModelError("pass", "The password is incorrect.");
                    return View(new User { Id = userId, Email = newEmail });
                }
                user.Email = newEmail; // Update the email address.
                db.Users.Update(user); // Update the user in the database.
                await db.SaveChangesAsync(); // Save changes to the database.
                TempData["Notification"] = "Change Email successfully";

                return RedirectToAction("UserInformation", new { id = user.Id });
            }
        }

        [SessionCheck]
        [HttpGet]
        public async Task<IActionResult> YourUpload(int? userId, int? page, int? pageSize)
        {
            // Check if the logged-in user ID does not match the provided user ID.
            var loggedInUserId = HttpContext.Session.GetString("UserID");
            if (userId == null || loggedInUserId == null || userId.ToString() != loggedInUserId)
            {
                return View("~/Views/Recipe/accessdenied.cshtml");
            }
            // Set default values for page and pageSize if not provided.
            if (page == null)
            {
                page = 1;
            }
            if (pageSize == null)
            {
                pageSize = 5;
            }
            if (userId == null)
            {
                return NotFound();
            }
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the user by ID.
                var guidances = db.Guidances
                .Where(g => g.UserId == userId);
                // Paginate the results.
                var listUpload = await guidances
                                 .OrderByDescending(b => b.Id)
                                 .Skip((page.Value - 1) * pageSize.Value)
                                 .Take(pageSize.Value)
                                 .ToListAsync();
                var totalCount = await guidances.CountAsync();
                var pagedList = new StaticPagedList<Guidance>(listUpload, page.Value, pageSize.Value, totalCount);
                ViewData["UserId"] = userId.Value; // Pass the user ID to the view.
                return View(pagedList);
            }
        }

        [SessionCheck]
        [HttpPost]
        public async Task<IActionResult> deleteYourUpload(int id)
        {
            // Create a new database context.
            using (var db = new JamesThewContext())
            {
                // Fetch the logged-in user.
                var userId = HttpContext.Session.GetString("UserID");
                // Fetch the guidance by ID.
                var user = await db.Users.FindAsync(int.Parse(userId));
                var guidance = await db.Guidances.FindAsync(id);
                // Return not found if the guidance does not exist.
                if (guidance == null)
                {
                    return NotFound();
                }
                // Check if the guidance belongs to the logged-in user or if the user is an admin.
                if (guidance.UserId != int.Parse(userId) && !user.Role)
                {
                    return View("~/Views/Recipe/accessdenied.cshtml");
                }
                // Remove the guidance.
                db.Guidances.Remove(guidance);
                string imageDirectoryPath = "";
                if (guidance.Type == false)
                {
                   imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "recipe", id.ToString());
                }
                else
                {
                   imageDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "postedImage", "tip", id.ToString());
                }
                Directory.Delete(imageDirectoryPath, true);
                // Save changes to the database.
                await db.SaveChangesAsync();
                // Set a success notification.
                TempData["Notification"] = "Guidance deleted successfully";
                return RedirectToAction("YourUpload", new { userId });
            }
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Homepage", "Home");
        }
    }
}