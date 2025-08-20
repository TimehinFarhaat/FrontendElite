using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult UserView()
    {
        // Pass any user-specific data if needed
        return View(); // /Views/Home/UserView.cshtml
    }

    public IActionResult AdminView()
    {
        // Pass any admin-specific data if needed
        return View(); // /Views/Home/AdminView.cshtml
    }

    private bool IsAdmin()
    {
        // Example check
        return User.Identity != null && User.IsInRole("Admin");
    }
}
