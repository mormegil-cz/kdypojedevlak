using System;
using System.Linq;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Djr;
using KdyPojedeVlak.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KdyPojedeVlak.Controllers
{
    public class AdminController : Controller
    {
        private const string PasswordCookieName = "MJPAdminPass";

        private readonly string correctPassword;
        private readonly DbModelContext dbModelContext;

        public AdminController(IConfiguration configuration, DbModelContext dbModelContext)
        {
            this.correctPassword = configuration["AdminPassword"];
            this.dbModelContext = dbModelContext;
        }

        // GET
        public IActionResult Index()
        {
            if (!IsLoggedIn)
            {
                return RedirectToAction("Login");
            }

            return View("Index");
        }

        public IActionResult Login(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                return IsLoggedIn ? RedirectToAction("Index") : View();
            }
            else
            {
                // TODO: FIXME! The redirect does not work!
                return new SetCookieResult(PasswordCookieName, password, new CookieOptions { Path = Url.RouteUrl(new { Controller = "Admin", Action = "Index" }), HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Strict, Secure = Request.IsHttps }, RedirectToAction("Index"));
            }
        }

        private bool IsLoggedIn
        {
            get
            {
                var passwordCookie = Request.Cookies[PasswordCookieName];
                return ConstantTimeEqual(passwordCookie, correctPassword);
            }
        }

        private static bool ConstantTimeEqual(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            var result = a.Select((t, i) => t == b[i] ? 0 : 1).Aggregate(0, (current, val) => current | val);
            return result == 0;
        }

        public IActionResult ExecuteAction(string action)
        {
            if (!IsLoggedIn || Request.Method != HttpMethods.Post)
            {
                return RedirectToAction("Index");
            }

            switch (action)
            {
                case "RenameAllCalendars":
                    DjrSchedule.RenameAllCalendars(dbModelContext);
                    InfoMessage.RegisterMessage(TempData, MessageClass.Success, "Pojmenování všech kalendářů přepočítáno");
                    return RedirectToAction("Index");

                case "RecomputeYearLimits":
                    DjrSchedule.RecomputeYearLimits(dbModelContext);
                    InfoMessage.RegisterMessage(TempData, MessageClass.Success, "Rozsahy všech roků přepočítány");
                    return RedirectToAction("Index");

                case "ReloadPointCoordinates":
                    DjrSchedule.ReloadPointCoordinates(dbModelContext);
                    InfoMessage.RegisterMessage(TempData, MessageClass.Success, "Souřadnice všech bodů načteny z číselníku");
                    return RedirectToAction("Index");

                default:
                    InfoMessage.RegisterMessage(TempData, MessageClass.Danger, "Cože?");
                    return RedirectToAction("Index");
            }
        }
    }
}