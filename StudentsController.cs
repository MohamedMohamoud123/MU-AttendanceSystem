using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Data.Entity;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class StudentsController : BaseController
    {
        private readonly MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ───────── helpers ───────── */
        private bool IsStudent() => (Session["RoleID"] as int?) == 3;
        private bool IsTeacher() => (Session["RoleID"] as int?) == 2;
        private bool IsAdmin() => (Session["RoleID"] as int?) == 1;
        private ActionResult Denied() => RedirectToAction("AccessDenied", "Account");

        // ───────── GET: Students ─────────
        public ActionResult Index(string searchQuery)
        {
            // Only Admin may view the list
            if (!IsAdmin())
                return Denied();

            var students = string.IsNullOrWhiteSpace(searchQuery)
                ? db.Students.ToList()
                : db.Students.Where(s =>
                    s.StudentRollID.ToString().Contains(searchQuery) ||
                    (s.FirstName + " " + s.MiddleName + " " + s.LastName)
                        .Contains(searchQuery))
                  .ToList();

            ViewBag.IsStudent = false;
            return View(students);
        }

        // ───────── GET: Students/Details/5 ─────────
        public ActionResult Details(int? id)
        {
            // Block Teachers
            if (IsTeacher())
                return Denied();

            if (IsStudent() && Session["UserID"] is int uid)
            {
                var me = db.Students.FirstOrDefault(s => s.UserID == uid);
                if (me == null) return Denied();
                return View(me);
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var student = db.Students.Find(id.Value);
            if (student == null) return HttpNotFound();
            return View(student);
        }

        // ───────── GET: Students/GetCourses/5 ─────────
        public ActionResult GetCourses(int id)
        {
            var courses = db.StudentCourses
                .Where(sc => sc.StudentID == id)
                .Select(sc => sc.Cours.CourseName)
                .ToList();
            return Json(courses, JsonRequestBehavior.AllowGet);
        }

        // ───────── CREATE (Admin only) ─────────
        public ActionResult Create()
        {
            if (!IsAdmin())
                return Denied();

            PopulateDropdowns();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Student student)
        {
            if (!IsAdmin())
                return Denied();

            if (db.Students.Any(s => s.StudentRollID == student.StudentRollID))
                ModelState.AddModelError("StudentRollID", "This Roll ID has already been used.");

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(student);
                return View(student);
            }

            if (student.User == null)
                student.User = new User();

            student.User.UserName = student.StudentRollID;
            student.User.Password = "123";
            student.User.IsFirstLogin = true;
            student.User.RoleID = 3;
            student.User.FullName = $"{student.FirstName} {student.MiddleName} {student.LastName}";
            student.User.CreatedDate = DateTime.Now;

            db.Users.Add(student.User);
            db.SaveChanges();

            student.UserID = student.User.UserID;
            student.CreatedDate = DateTime.Now;
            student.Status = string.IsNullOrEmpty(student.Status) ? "Active" : student.Status;

            db.Students.Add(student);
            db.SaveChanges();

            var matchingCourses = db.Courses
                .Where(c =>
                    c.FacultyID == student.FacultyID &&
                    c.DepartmentID == student.DepartmentID &&
                    c.SemesterID == student.SemesterID)
                .ToList();

            foreach (var course in matchingCourses)
            {
                db.StudentCourses.Add(new StudentCours
                {
                    StudentID = student.StudentID,
                    CourseID = course.CourseID,
                    FacultyID = course.FacultyID,
                    DepartmentID = course.DepartmentID,
                    SemesterID = course.SemesterID,
                    CreatedDate = DateTime.Now
                });
            }
            db.SaveChanges();

            var courseIds = matchingCourses.Select(c => c.CourseID).ToList();
            var rawBatches = db.Attendances
                .Where(a =>
                    courseIds.Contains(a.CourseID) &&
                    a.FacultyID == student.FacultyID &&
                    a.DepartmentID == student.DepartmentID &&
                    a.SemesterID == student.SemesterID &&
                    a.AcademicYearID == student.AcademicYearID)
                .GroupBy(a => new {
                    a.CourseID,
                    a.TeacherID,
                    a.FacultyID,
                    a.DepartmentID,
                    a.SemesterID,
                    a.AcademicYearID,
                    a.StartMonth,
                    a.EndMonth
                })
                .Select(g => g.FirstOrDefault())
                .ToList();

            foreach (var batch in rawBatches)
            {
                bool exists = db.Attendances.Any(a =>
                    a.StudentID == student.StudentID &&
                    a.CourseID == batch.CourseID &&
                    a.StartMonth == batch.StartMonth &&
                    a.EndMonth == batch.EndMonth);

                if (!exists)
                {
                    db.Attendances.Add(new Attendance
                    {
                        StudentID = student.StudentID,
                        TeacherID = batch.TeacherID,
                        CourseID = batch.CourseID,
                        FacultyID = batch.FacultyID,
                        DepartmentID = batch.DepartmentID,
                        SemesterID = batch.SemesterID,
                        AcademicYearID = batch.AcademicYearID,
                        StartMonth = batch.StartMonth,
                        EndMonth = batch.EndMonth,
                        HoursPresent = 0,
                        HoursAbsent = 0,
                        FinalStatus = "Allowed",
                        CreatedDate = DateTime.Now
                    });
                }
            }
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // ───────── EDIT (Admin only) ─────────
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin())
                return Denied();

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var student = db.Students.Find(id.Value);
            if (student == null) return HttpNotFound();

            PopulateDropdowns(student);
            return View(student);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include =
            "StudentID,UserID,StudentRollID,FirstName,MiddleName,LastName,Status,FacultyID,DepartmentID,SemesterID,AcademicYearID,CreatedDate")]
            Student student)
        {
            if (!IsAdmin())
                return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(student).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            PopulateDropdowns(student);
            return View(student);
        }

        // ───────── DELETE (Admin & Teacher) ─────────
        public ActionResult Delete(int? id)
        {
            if (!(IsAdmin() || IsTeacher()))
                return Denied();

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var student = db.Students.Find(id.Value);
            if (student == null) return HttpNotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!(IsAdmin() || IsTeacher()))
                return Denied();

            var student = db.Students.Find(id);
            if (student == null) return HttpNotFound();

            db.AttendanceDetails.Where(d => d.StudentID == id).ToList()
              .ForEach(d => db.AttendanceDetails.Remove(d));
            db.Attendances.Where(a => a.StudentID == id).ToList()
              .ForEach(a => db.Attendances.Remove(a));
            db.StudentCourses.Where(sc => sc.StudentID == id).ToList()
              .ForEach(sc => db.StudentCourses.Remove(sc));

            db.Students.Remove(student);
            var user = db.Users.Find(student.UserID);
            if (user != null) db.Users.Remove(user);

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ───────── AJAX helpers ─────────
        public JsonResult GetDepartmentsByFaculty(int facultyId)
        {
            var deps = db.Departments
                         .Where(d => d.FacultyID == facultyId)
                         .Select(d => new { d.DepartmentID, d.DepartmentName })
                         .ToList();
            return Json(deps, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSemestersByAcademicYear(int yearId)
        {
            var sems = db.Semesters
                         .Where(s => s.AcademicYearID == yearId)
                         .Select(s => new { s.SemesterID, s.SemesterName })
                         .ToList();
            return Json(sems, JsonRequestBehavior.AllowGet);
        }

        // ───────── dropdown helper ─────────
        private void PopulateDropdowns(Student student = null)
        {
            int facultyId = student?.FacultyID ?? 0;
            int academicYearId = student?.AcademicYearID ?? 0;

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", student?.FacultyID);
            ViewBag.DepartmentID = new SelectList(
                db.Departments.Where(d => d.FacultyID == facultyId),
                "DepartmentID", "DepartmentName",
                student?.DepartmentID);
            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", student?.AcademicYearID);
            ViewBag.SemesterID = new SelectList(
                db.Semesters.Where(s => s.AcademicYearID == academicYearId),
                "SemesterID", "SemesterName",
                student?.SemesterID);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
