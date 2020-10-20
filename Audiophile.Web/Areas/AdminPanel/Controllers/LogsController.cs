using Audiophile.Services;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class LogsController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var logService=new LogServices())
            { 
                var log = logService.GetLog500();
                return View(log);
            }
            
        }
    }
}