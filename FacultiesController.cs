using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;
using MU_AttendanceSystem.Validation;

namespace MU_AttendanceSystem.Controllers
{
    public class FacultiesController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────── role check & redirect ─────────── */
        private bool IsAdmin()
            => String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        // GET: Faculties
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            if (string.IsNullOrEmpty(searchQuery))
            {
                return View(db.Faculties.ToList());
            }

            var filteredFaculties = db.Faculties
                .Where(f => f.FacultyName.Contains(searchQuery))
                .ToList();

            return View(filteredFaculties);
        }

        // GET: Faculties/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Faculty faculty = db.Faculties.Find(id.Value);
            if (faculty == null) return HttpNotFound();

            return View(faculty);
        }

        // GET: Faculties/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();
            return View();
        }

        // POST: Faculties/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FacultyID,FacultyName,CreatedDate")] FacultiedDto model)
        {
            if (!IsAdmin()) return Denied();

            var faculty = new Faculty
            {
                FacultyID = model.FacultyID,
                FacultyName = model.FacultyName,
                CreatedDate = DateTime.Now
            };

            if (ModelState.IsValid)
            {
                db.Faculties.Add(faculty);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(faculty);
        }

        // GET: Faculties/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Faculty faculty = db.Faculties.Find(id.Value);
            if (faculty == null) return HttpNotFound();

            return View(faculty);
        }

        // POST: Faculties/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FacultyID,FacultyName,CreatedDate")] FacultiedDto model)
        {
            if (!IsAdmin()) return Denied();

            var faculty = new Faculty
            {
                FacultyID = model.FacultyID,
                FacultyName = model.FacultyName,
                CreatedDate = DateTime.Now
            };

            if (ModelState.IsValid)
            {
                db.Entry(faculty).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(faculty);
        }

        // GET: Faculties/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Faculty faculty = db.Faculties.Find(id.Value);
            if (faculty == null) return HttpNotFound();

            return View(faculty);
        }

        // POST: Faculties/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            Faculty faculty = db.Faculties.Find(id);
            db.Faculties.Remove(faculty);
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
