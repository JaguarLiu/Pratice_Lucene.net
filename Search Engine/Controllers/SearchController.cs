using Search_Engine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Search_Engine.Controllers
{
    public class SearchController : Controller
    {
        // GET: Search
        [HttpGet]
        public ActionResult Index()
        {
            if (!Directory.Exists(LuceneService._luceneDir)) Directory.CreateDirectory(LuceneService._luceneDir);
            LuceneService.Init();
            return View();
        }
        [HttpPost]
        public ActionResult Index(string searchstring)
        {
            var model = LuceneService.Search<Product>(searchstring);
            return View(model);
        }
    }
}