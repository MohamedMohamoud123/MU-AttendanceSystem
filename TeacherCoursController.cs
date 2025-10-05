using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class TeacherCoursController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────── role check & redirect ─────────── */
        private bool IsAdmin() =>
            String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied() =>
            RedirectToAction("AccessDenied", "Account");

        // ───────── GET: TeacherCours ─────────
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            var query = db.TeacherCourses
                          .Include(tc => tc.Teacher)
                          .Include(tc => tc.Cours)
                          .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(tc =>
                    tc.Teacher.TeacherRollID.Contains(searchQuery) ||
                    (tc.Teacher.FirstName + " " +
                     tc.Teacher.MiddleName + " " +
                     tc.Teacher.LastName).Contains(searchQuery) ||
                    tc.Cours.CourseName.Contains(searchQuery)
                );
            }

            var list = query.ToList();
            return View(list);
        }

        // ───────── GET: TeacherCours/Details/5 ─────────
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var teacherCours = db.TeacherCourses.Find(id.Value);
            if (teacherCours == null) return HttpNotFound();

            return View(teacherCours);
        }

        // ───────── GET: TeacherCours/Create ─────────
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName");
            ViewBag.DepartmentID = new SelectList(Enumerable.Empty<Department>(), "DepartmentID", "DepartmentName");
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName");
            ViewBag.CourseID = new SelectList(Enumerable.Empty<Cours>(), "CourseID", "CourseName");
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherRollID");
            return View();
        }

        // ───────── POST: TeacherCours/Create ─────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TeacherCourseID,TeacherID,CourseID,FacultyID,DepartmentID,SemesterID,CreatedDate")] TeacherCours teacherCours)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.TeacherCourses.Add(teacherCours);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", teacherCours.FacultyID);
            ViewBag.DepartmentID = new SelectList(db.Departments
                                                      .Where(d => d.FacultyID == teacherCours.FacultyID),
                                                  "DepartmentID", "DepartmentName", teacherCours.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", teacherCours.SemesterID);
            ViewBag.CourseID = new SelectList(db.Courses
                                                      .Where(c =>
                                                          c.FacultyID == teacherCours.FacultyID &&
                                                          c.DepartmentID == teacherCours.DepartmentID &&
                                                          c.SemesterID == teacherCours.SemesterID),
                                                  "CourseID", "CourseName", teacherCours.CourseID);
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherRollID", teacherCours.TeacherID);
            return View(teacherCours);
        }

        // ───────── GET: TeacherCours/Edit/5 ─────────
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var teacherCours = db.TeacherCourses.Find(id.Value);
            if (teacherCours == null) return HttpNotFound();

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", teacherCours.FacultyID);
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName", teacherCours.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", teacherCours.SemesterID);
            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", teacherCours.CourseID);
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherRollID", teacherCours.TeacherID);
            return View(teacherCours);
        }

        // ───────── POST: TeacherCours/Edit/5 ─────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TeacherCourseID,TeacherID,CourseID,FacultyID,DepartmentID,SemesterID,CreatedDate")] TeacherCours teacherCours)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(teacherCours).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", teacherCours.FacultyID);
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "DepartmentName", teacherCours.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters, "SemesterID", "SemesterName", teacherCours.SemesterID);
            ViewBag.CourseID = new SelectList(db.Courses, "CourseID", "CourseName", teacherCours.CourseID);
            ViewBag.TeacherID = new SelectList(db.Teachers, "TeacherID", "TeacherRollID", teacherCours.TeacherID);
            return View(teacherCours);
        }

        // ───────── GET: TeacherCours/Delete/5 ─────────
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var teacherCours = db.TeacherCourses.Find(id.Value);
            if (teacherCours == null) return HttpNotFound();

            return View(teacherCours);
        }

        // ───────── POST: TeacherCours/Delete/5 ─────────
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            var teacherCours = db.TeacherCourses.Find(id);
            db.TeacherCourses.Remove(teacherCours);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ───────── JSON: GetDepartments ─────────
        public JsonResult GetDepartments(int facultyId)
        {
            if (!IsAdmin()) return Json(new { error = "Access denied" }, JsonRequestBehavior.AllowGet);

            var list = db.Departments
                         .Where(d => d.FacultyID == facultyId)
                         .Select(d => new {
                             d.DepartmentID,
                             d.DepartmentName
                         })
                         .ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // ───────── JSON: GetCourses ─────────
        public JsonResult GetCourses(int facultyId, int departmentId, int semesterId)
        {
            if (!IsAdmin()) return Json(new { error = "Access denied" }, JsonRequestBehavior.AllowGet);

            var list = db.Courses
                         .Where(c =>
                             c.FacultyID == facultyId &&
                             c.DepartmentID == departmentId &&
                             c.SemesterID == semesterId)
                         .Select(c => new {
                             c.CourseID,
                             c.CourseName
                         })
                         .ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
