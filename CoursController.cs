using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;
using MU_AttendanceSystem.Validation;

namespace MU_AttendanceSystem.Controllers
{
    public class CoursController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ──────────── role check & redirect ──────────── */
        private bool IsAdmin()
            => String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        // GET: Cours
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            if (string.IsNullOrEmpty(searchQuery))
                return View(db.Courses.ToList());

            var filteredCourses = db.Courses
                .Where(f => f.CourseName.Contains(searchQuery))
                .ToList();

            return View(filteredCourses);
        }

        // GET: Cours/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Cours cours = db.Courses.Find(id.Value);
            if (cours == null) return HttpNotFound();

            return View(cours);
        }

        // GET: Cours/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName");
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName");
            ViewBag.DepartmentID = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewBag.SemesterID = new SelectList(Enumerable.Empty<SelectListItem>());
            return View();
        }

        // POST: Cours/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CourseID,CourseName,CreditHours,FacultyID,DepartmentID,AcademicYearID,SemesterID,CreatedDate")] CoursDto model)
        {
            var cours = new Cours()
            {
                CourseID = model.CourseID,
                CourseName = model.CourseName,
                CreditHours = model.CreditHours,
                FacultyID = model.FacultyID,
                DepartmentID = model.DepartmentID,
                AcademicYearID= model.AcademicYearID,
                SemesterID= model.SemesterID,
                CreatedDate = DateTime.Now
            };
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Courses.Add(cours);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", cours.AcademicYearID);
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", cours.FacultyID);
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName", cours.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", cours.SemesterID);
            return View(cours);
        }

        // GET: Cours/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Cours cours = db.Courses.Find(id.Value);
            if (cours == null) return HttpNotFound();

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", cours.AcademicYearID);
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", cours.FacultyID);
            ViewBag.DepartmentID = new SelectList(db.Departments.Where(d => d.FacultyID == cours.FacultyID), "DepartmentID", "DepartmentName", cours.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters.Where(s => s.AcademicYearID == cours.AcademicYearID), "SemesterID", "SemesterName", cours.SemesterID);
            return View(cours);
        }

        // POST: Cours/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CourseID,CourseName,CreditHours,FacultyID,DepartmentID,AcademicYearID,SemesterID,CreatedDate")] CoursDto model)
        {
            var cours = new Cours()
            {
                CourseID = model.CourseID,
                CourseName = model.CourseName,
                CreditHours = model.CreditHours,
                FacultyID = model.FacultyID,
                DepartmentID = model.DepartmentID,
                AcademicYearID = model.AcademicYearID,
                SemesterID = model.SemesterID,
                CreatedDate = DateTime.Now
            };
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(cours).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", cours.AcademicYearID);
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", cours.FacultyID);
            ViewBag.DepartmentID = new SelectList(db.Departments.Where(d => d.FacultyID == cours.FacultyID), "DepartmentID", "DepartmentName", cours.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters.Where(s => s.AcademicYearID == cours.AcademicYearID), "SemesterID", "SemesterName", cours.SemesterID);
            return View(cours);
        }

        // AJAX: get departments by faculty
        public JsonResult GetDepartmentsByFaculty(int facultyId)
        {
            if (!IsAdmin()) return Json(new { }, JsonRequestBehavior.AllowGet);

            var departments = db.Departments
                                .Where(d => d.FacultyID == facultyId)
                                .Select(d => new
                                {
                                    d.DepartmentID,
                                    d.DepartmentName
                                })
                                .ToList();

            return Json(departments, JsonRequestBehavior.AllowGet);
        }

        // AJAX: get semesters by academic year
        public JsonResult GetSemestersByAcademicYear(int academicYearId)
        {
            if (!IsAdmin()) return Json(new { }, JsonRequestBehavior.AllowGet);

            var semesters = db.Semesters
                              .Where(s => s.AcademicYearID == academicYearId)
                              .Select(s => new
                              {
                                  s.SemesterID,
                                  s.SemesterName
                              })
                              .ToList();

            return Json(semesters, JsonRequestBehavior.AllowGet);
        }

        // GET: Cours/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Cours cours = db.Courses.Find(id.Value);
            if (cours == null) return HttpNotFound();

            return View(cours);
        }

        // POST: Cours/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            Cours cours = db.Courses.Find(id);
            db.Courses.Remove(cours);
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
