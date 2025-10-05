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
    public class RolesController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ──────────── role check & redirect ──────────── */
        private bool IsAdmin()
            => String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        // GET: Roles
        public ActionResult Index()
        {
            if (!IsAdmin()) return Denied();
            return View(db.Roles.ToList());
        }

        // GET: Roles/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Role role = db.Roles.Find(id.Value);
            if (role == null) return HttpNotFound();
            return View(role);
        }

        // GET: Roles/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();
            return View();
        }

        // POST: Roles/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RoleID,RoleName,CreatedDate")] Role role)
        {
            if (!IsAdmin()) return Denied();
            if (ModelState.IsValid)
            {
                db.Roles.Add(role);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(role);
        }

        // GET: Roles/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Role role = db.Roles.Find(id.Value);
            if (role == null) return HttpNotFound();
            return View(role);
        }

        // POST: Roles/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RoleID,RoleName,CreatedDate")] Role role)
        {
            if (!IsAdmin()) return Denied();
            if (ModelState.IsValid)
            {
                db.Entry(role).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(role);
        }

        // GET: Roles/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Role role = db.Roles.Find(id.Value);
            if (role == null) return HttpNotFound();
            return View(role);
        }

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();
            Role role = db.Roles.Find(id);
            db.Roles.Remove(role);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
