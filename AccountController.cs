// File: Controllers/AccountController.cs
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class AccountController : Controller
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        // GET: /Account/Login
        public ActionResult Login()
        {
            return View(new User());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User model)
        {
            // 1) Lockout check
            DateTime? lockoutUntil = Session["LockoutTime"] as DateTime?;
            int failedAttempts = (int)(Session["FailedLoginAttempts"] ?? 0);

            if (lockoutUntil.HasValue && lockoutUntil > DateTime.Now)
            {
                ViewBag.LockoutUntil = lockoutUntil.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                ModelState.AddModelError("", "Too many failed attempts. You are locked out.");
                return View(model);
            }

            // 2) Validate
            if (!ModelState.IsValid)
                return View(model);

            // 3) Authenticate (eager-load Role)
            var user = db.Users
                         .Include(u => u.Role)
                         .FirstOrDefault(u =>
                             u.UserName == model.UserName &&
                             u.Password == model.Password
                         );

            if (user == null)
            {
                // failed login → increment counter
                failedAttempts++;
                Session["FailedLoginAttempts"] = failedAttempts;

                if (failedAttempts >= 5)
                {
                    var until = DateTime.Now.AddMinutes(15);
                    Session["LockoutTime"] = until;
                    Session["FailedLoginAttempts"] = 0;
                    ViewBag.LockoutUntil = until.ToString("yyyy-MM-ddTHH:mm:ss");
                    ModelState.AddModelError("", "Too many failed attempts. Locked out for 15 minutes.");
                }
                else
                {
                    ModelState.AddModelError("", $"Invalid credentials. Attempts left: {5 - failedAttempts}");
                }
                return View(model);
            }

            // 4) Success → reset lockout & set session
            Session["FailedLoginAttempts"] = 0;
            Session["LockoutTime"] = null;

            Session["UserID"] = user.UserID;
            Session["UserName"] = user.UserName;
            Session["FullName"] = user.FullName;
            Session["RoleID"] = user.RoleID;
            Session["RoleName"] = user.Role.RoleName;

            // 5) First-time login?
            if (user.IsFirstLogin)
                return RedirectToAction("ChangePassword");

            // 6) Redirect by RoleName
            switch (user.Role.RoleName)
            {
                case "Student":
                    return RedirectToAction("StudentDash", "Dashboard");
                case "Teacher":
                    return RedirectToAction("TeacherDash", "Dashboard");
                case "Admin":    // your “staff” users
                    return RedirectToAction("StaffDash", "Dashboard");
                default:
                    return RedirectToAction("AccessDenied");
            }
        }

        // GET: /Account/ChangePassword
        public ActionResult ChangePassword()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Both password fields are required.";
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }
            if (newPassword == "123")
            {
                ViewBag.Error = "You cannot use the default password '123'.";
                return View();
            }

            int uid = (int)Session["UserID"];
            var user = db.Users.Find(uid);
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View();
            }

            user.Password = newPassword;
            user.IsFirstLogin = false;
            db.SaveChanges();

            // Redirect back according to RoleName
            switch (Session["RoleName"] as string)
            {
                case "Student":
                    return RedirectToAction("StudentDash", "Dashboard");
                case "Teacher":
                    return RedirectToAction("TeacherDash", "Dashboard");
                case "Admin":
                    return RedirectToAction("StaffDash", "Dashboard");
                default:
                    return RedirectToAction("AccessDenied");
            }
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            // Clear everything
            Session.Clear();
            Session.Abandon();

            // Also expire the cache in the response
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            return RedirectToAction("Login");
        }


        // GET: /Account/AccessDenied
        public ActionResult AccessDenied()
        {
            return View("AccessDenied");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
