using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class AdminController : Controller
{
    private readonly AdminApiClient _adminApiClient;

    public AdminController(AdminApiClient adminApiClient)
    {
        _adminApiClient = adminApiClient;
    }

    // GET: /Admin/Login
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl; // store return URL
        if (TempData.ContainsKey("Error"))
            ViewBag.Error = TempData["Error"];

        return View();
    }

    // POST: /Admin/Login
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Json(new { success = false, message = "Username and password are required." });

        var result = await _adminApiClient.LoginAsync(username, password);

        if (result.IsSuccess)
        {
            HttpContext.Session.SetString("IsAdmin", "true"); // mark admin logged in
            return Json(new { success = true, message = "Login successful.", returnUrl });
        }

        return Json(new { success = false, message = result.Message ?? "Invalid username or password." });
    }

    // GET: /Admin/Logout
    
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var result = await _adminApiClient.LogoutAsync();
        if (result)
        {
            HttpContext.Session.SetString("IsAdmin", "false"); 
          TempData["Success"] = "Logged out successfully";
        }
        else
        {
            TempData["Error"] = "Logout failed";
        }

        return RedirectToAction("Index", "Home"); // redirect to homepage
    }
}
