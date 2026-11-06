using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freshify.Controllers
{
    [AllowAnonymous]
    public class AboutController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}