using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class StudentCoursController : BaseController
    {
        private readonly MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────── role checks & redirect ─────────── */
        private bool IsAdmin() =>
            String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied() =>
            RedirectToAction("AccessDenied", "Account");

        // GET: StudentCours
        public ActionResult Index()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.IsStudent = false;
            var allCourses = db.StudentCourses
                               .Include(sc => sc.Cours)
                               .Include(sc => sc.Department)
                               .Include(sc => sc.Faculty)
                               .Include(sc => sc.Semester)
                               .Include(sc => sc.Student)
                               .ToList();
            return View(allCourses);
        }

        // GET: StudentCours/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var sc = db.StudentCourses.Find(id.Value);
            if (sc == null) return HttpNotFound();

            return View(sc);
        }

        // GET: StudentCours/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName");
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName");
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName");
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName");
            ViewBag.StudentID = new SelectList(db.Students, "StudentID", "StudentRollID");
            return View();
        }

        // POST: StudentCours/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "StudentCourseID,StudentID,CourseID,FacultyID,DepartmentID,SemesterID,CreatedDate")] StudentCours studentCours)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.StudentCourses.Add(studentCours);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", studentCours.CourseID);
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName", studentCours.DepartmentID);
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", studentCours.FacultyID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", studentCours.SemesterID);
            ViewBag.StudentID = new SelectList(db.Students, "StudentID", "StudentRollID", studentCours.StudentID);
            return View(studentCours);
        }

        // GET: StudentCours/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var sc = db.StudentCourses.Find(id.Value);
            if (sc == null) return HttpNotFound();

            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", sc.CourseID);
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName", sc.DepartmentID);
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", sc.FacultyID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", sc.SemesterID);
            ViewBag.StudentID = new SelectList(db.Students, "StudentID", "StudentRollID", sc.StudentID);
            return View(sc);
        }

        // POST: StudentCours/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StudentCourseID,StudentID,CourseID,FacultyID,DepartmentID,SemesterID,CreatedDate")] StudentCours studentCours)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(studentCours).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", studentCours.CourseID);
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName", studentCours.DepartmentID);
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", studentCours.FacultyID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", studentCours.SemesterID);
            ViewBag.StudentID = new SelectList(db.Students, "StudentID", "StudentRollID", studentCours.StudentID);
            return View(studentCours);
        }

        // GET: StudentCours/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var sc = db.StudentCourses.Find(id.Value);
            if (sc == null) return HttpNotFound();

            return View(sc);
        }

        // POST: StudentCours/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            var sc = db.StudentCourses.Find(id);
            db.StudentCourses.Remove(sc);
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
