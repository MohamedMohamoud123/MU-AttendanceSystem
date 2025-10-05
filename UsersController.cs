using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class UsersController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────── role check & redirect ─────────── */
        private bool IsAdmin() =>
            (Session["RoleID"] as int?) == 1;

        private ActionResult Denied() =>
            RedirectToAction("AccessDenied", "Account");

        // GET: Users
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            // start with all users
            var users = db.Users.AsQueryable();

            // if there's a search term, filter on UserName or FullName
            if (!String.IsNullOrWhiteSpace(searchQuery))
            {
                users = users.Where(u =>
                    u.UserName.Contains(searchQuery) ||
                    u.FullName.Contains(searchQuery)
                );
            }

            return View(users.ToList());
        }

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id.Value);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserID,UserName,Password,RoleID,FullName,CreatedDate,IsFirstLogin")] User user)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id.Value);
            if (user == null)
                return HttpNotFound();

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserID,UserName,Password,RoleID,FullName,CreatedDate,IsFirstLogin")] User user)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RoleID = new SelectList(db.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Find(id.Value);
            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            var user = db.Users.Find(id);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
