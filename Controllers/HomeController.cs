using System.Linq;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Models;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Controllers
{
    public class HomeController : Controller
    {
        private readonly DbModelContext dbModelContext;

        public HomeController(DbModelContext dbModelContext)
        {
            this.dbModelContext = dbModelContext;
        }

        public IActionResult Index()
        {
            var latestImport = dbModelContext.ImportedFiles.Max(f => f.ImportTime);
            var newestData = dbModelContext.ImportedFiles.Max(f => f.CreationDate);
            return View(new VersionInformation
            {
                // TODO: LastDownload from CisjrUpdater
                LastDownload = latestImport,
                LatestImport = latestImport,
                NewestData = newestData
            });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}