using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;
using MU_AttendanceSystem.Validation;

namespace MU_AttendanceSystem.Controllers
{
    public class SemestersController : BaseController
    {
        private readonly MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ──────────── role check & redirect ──────────── */
        private bool IsAdmin()
            => String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        // GET: Semesters
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            if (string.IsNullOrEmpty(searchQuery))
                return View(db.Semesters.ToList());

            var filtered = db.Semesters
                             .Where(s => s.SemesterName.Contains(searchQuery))
                             .ToList();
            return View(filtered);
        }

        // GET: Semesters/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var semester = db.Semesters.Find(id.Value);
            if (semester == null) return HttpNotFound();

            return View(semester);
        }

        // GET: Semesters/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName");
            return View();
        }

        // POST: Semesters/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "SemesterID,SemesterName,AcademicYearID,CreatedDate")] SemesterDto model)
        {
            if (!IsAdmin()) return Denied();

            var semester = new Semester()
            {
                SemesterID = model.SemesterID,
                SemesterName = model.SemesterName,
                AcademicYearID=model.AcademicYearID,
                CreatedDate = DateTime.Now,
            };
            if (ModelState.IsValid)
            {
                db.Semesters.Add(semester);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", semester.AcademicYearID);
            return View(semester);
        }

        // GET: Semesters/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var semester = db.Semesters.Find(id.Value);
            if (semester == null) return HttpNotFound();

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", semester.AcademicYearID);
            return View(semester);
        }

        // POST: Semesters/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "SemesterID,SemesterName,AcademicYearID,CreatedDate")] SemesterDto model)
        {
            if (!IsAdmin()) return Denied();

            var semester = new Semester()
            {
                SemesterID = model.SemesterID,
                SemesterName = model.SemesterName,
                AcademicYearID = model.AcademicYearID,
                CreatedDate = DateTime.Now,
            };
            if (ModelState.IsValid)
            {
                db.Entry(semester).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", semester.AcademicYearID);
            return View(semester);
        }

        // GET: Semesters/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var semester = db.Semesters.Find(id.Value);
            if (semester == null) return HttpNotFound();

            return View(semester);
        }

        // POST: Semesters/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            var semester = db.Semesters.Find(id);
            db.Semesters.Remove(semester);
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
