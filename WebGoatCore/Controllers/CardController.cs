using WebGoatCore.Models;
using WebGoatCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

using System.IO;

namespace WebGoatCore.Controllers
{
    [Route("[controller]/[action]")]
    public class CardController : Controller
    {

        public CardController()
        {
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Update() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(string contents)
        {
            var path = @"./StoredCreditCards.xml";
            using (StreamWriter sw = new StreamWriter(path))
            {
             sw.Write(contents);
            }
            return RedirectToAction("Update");
        }

    }
}