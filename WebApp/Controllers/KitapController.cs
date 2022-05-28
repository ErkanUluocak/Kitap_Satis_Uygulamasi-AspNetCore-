using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class KitapController : Controller
    {
        ApplicationDbContext _db;
        UserManager<Uye> _userMan;
        public KitapController(ApplicationDbContext db, UserManager<Uye> userMan)
        {
            _db = db;
            //db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // _userMan = userMan;
            // AdminYap();


        }

        private async void AdminYap() {
            Uye uye = _userMan.FindByIdAsync("1").Result;
            await _userMan.AddToRoleAsync(uye, "Admin");
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
