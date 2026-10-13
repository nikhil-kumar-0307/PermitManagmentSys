using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    public class AccountController : Controller
    {
        // TODO: replace with your real DbContext (must expose DbSet<User> Users)
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: /Account/Login
        [HttpGet, AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToLocal(returnUrl, GetRole());

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginDto());
        }

        // POST: /Account/Login
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Login(LoginDto model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var username = model.Username.Trim();
            var password = model.Password;

            // Plain password comparison (case-sensitive, done in memory)
            var user = _db.Users
                .Where(u => u.Username == username)
                .ToList()
                .FirstOrDefault(u => string.Equals(u.Password, password, StringComparison.Ordinal));

            // One generic message so the screen never reveals which part was wrong.
            if (user == null)
            {
                ModelState.AddModelError("", "Username or password is incorrect.");
                model.Password = null;
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "This account is disabled. Contact your administrator.");
                model.Password = null;
                return View(model);
            }

            user.LastLoginOn = DateTime.Now;
            _db.SaveChanges();

            // MULTI-DEVICE LOGIN:
            // Each sign-in creates its own independent auth cookie on that browser/device.
            // Nothing is stored on the server that limits a user to one session,
            // so the same user can be signed in on any number of devices at once.
            SignIn(user, model.RememberMe);

            return RedirectToLocal(returnUrl, user.Role);
        }

        // POST: /Account/Logout  (signs out this device only - other devices stay signed in)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        // ---------- helpers ----------

        private void SignIn(User user, bool remember)
        {
            var now = DateTime.Now;
            var expires = remember ? now.AddDays(14) : now.AddHours(8);

            // userData = Id|Role|FullName  (read back in Global.asax)
            var userData = string.Format("{0}|{1}|{2}", user.Id, (user.Role ?? "").Trim(), user.FullName.Replace("|", " "));

            var ticket = new FormsAuthenticationTicket(
                2, user.Username, now, expires, remember, userData, FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Path = FormsAuthentication.FormsCookiePath
            };
            if (remember) cookie.Expires = expires;   // persistent cookie only when "Remember me" is ticked

            Response.Cookies.Add(cookie);
        }

        private string GetRole()
        {
            return User.IsInRole(AppRoles.Operation) ? AppRoles.Operation : null;
        }

        private ActionResult RedirectToLocal(string returnUrl, string role)
        {
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/" && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (string.Equals((role ?? "").Trim(), AppRoles.Operation, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Operation");

            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}