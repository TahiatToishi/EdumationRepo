using Microsoft.AspNetCore.Mvc;

namespace EduMation.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}