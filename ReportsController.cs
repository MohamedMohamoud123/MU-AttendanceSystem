using System.Linq;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class ReportsController : BaseController
    {
        private readonly MU_AttendanceSystemDBEntities _db = new MU_AttendanceSystemDBEntities();

        // GET /Reports → redirect by RoleName
        [HttpGet]
        public ActionResult Index()
        {
            var userId = Session["UserID"] as int?;
            var roleName = Session["RoleName"] as string;
            if (userId == null)
                return RedirectToAction("Login", "Account");

            switch (roleName)
            {
                case "Staff":
                case "Admin":
                    return RedirectToAction("StaffIndex");
                case "Teacher":
                    return RedirectToAction("TeacherIndex");
                case "Student":
                    return RedirectToAction("StudentIndex");
                default:
                    return RedirectToAction("AccessDenied", "Account");
            }
        }

        // GET /Reports/StaffIndex
        [HttpGet]
        public ActionResult StaffIndex()
        {
            var userId = Session["UserID"] as int?;
            var roleName = Session["RoleName"] as string;
            if (userId == null) return RedirectToAction("Login", "Account");
            if (roleName != "Staff" && roleName != "Admin")
                return RedirectToAction("AccessDenied", "Account");

            ViewBag.Faculties = new SelectList(_db.Faculties, "FacultyID", "FacultyName");
            ViewBag.AcademicYears = new SelectList(_db.AcademicYears, "AcademicYearID", "YearName");
            return View();  // Views/Reports/StaffIndex.cshtml
        }

        // GET /Reports/TeacherIndex
        [HttpGet]
        public ActionResult TeacherIndex()
        {
            var userId = Session["UserID"] as int?;
            var roleName = Session["RoleName"] as string;
            if (userId == null) return RedirectToAction("Login", "Account");
            if (roleName != "Teacher")
                return RedirectToAction("AccessDenied", "Account");

            var teacher = _db.Teachers.FirstOrDefault(t => t.UserID == userId.Value);
            if (teacher == null)
                return RedirectToAction("AccessDenied", "Account");

            var courses = _db.TeacherCourses
                             .Where(tc => tc.TeacherID == teacher.TeacherID)
                             .Select(tc => tc.Cours)
                             .Distinct()
                             .ToList();

            ViewBag.Courses = new SelectList(courses, "CourseID", "CourseName");
            return View();  // Views/Reports/TeacherIndex.cshtml
        }

        // GET /Reports/StudentIndex
        [HttpGet]
        public ActionResult StudentIndex()
        {
            var userId = Session["UserID"] as int?;
            var roleName = Session["RoleName"] as string;
            if (userId == null) return RedirectToAction("Login", "Account");
            if (roleName != "Student")
                return RedirectToAction("AccessDenied", "Account");

            var student = _db.Students.FirstOrDefault(s => s.UserID == userId.Value);
            if (student == null)
                return RedirectToAction("AccessDenied", "Account");

            var courses = _db.Attendances
                             .Where(a => a.StudentID == student.StudentID)
                             .Select(a => a.Cours)
                             .Distinct()
                             .ToList();

            ViewBag.Courses = new SelectList(courses, "CourseID", "CourseName");
            return View();  // Views/Reports/StudentIndex.cshtml
        }

        // GET /Reports/GetNotAllowedData
        [HttpGet]
        public JsonResult GetNotAllowedData(
            int? facultyId,
            int? departmentId,
            int? yearId,
            int? semesterId,
            int? studentId,
            int? courseId)
        {
            var userId = Session["UserID"] as int?;
            var roleName = Session["RoleName"] as string;
            if (userId == null)
                return Json(new { error = "NotLoggedIn" }, JsonRequestBehavior.AllowGet);

            var q = _db.Attendances.Where(a => a.FinalStatus == "Not Allowed");

            // Role restrictions
            if (roleName == "Teacher")
            {
                var teacher = _db.Teachers.FirstOrDefault(t => t.UserID == userId.Value);
                if (teacher == null)
                    return Json(new { error = "AccessDenied" }, JsonRequestBehavior.AllowGet);

                var allowedCourses = _db.TeacherCourses
                                        .Where(tc => tc.TeacherID == teacher.TeacherID)
                                        .Select(tc => tc.CourseID)
                                        .ToList();
                q = q.Where(a => allowedCourses.Contains(a.CourseID));
            }
            else if (roleName == "Student")
            {
                var student = _db.Students.FirstOrDefault(s => s.UserID == userId.Value);
                if (student == null)
                    return Json(new { error = "AccessDenied" }, JsonRequestBehavior.AllowGet);

                q = q.Where(a => a.StudentID == student.StudentID);
            }
            else if (roleName != "Staff" && roleName != "Admin")
            {
                return Json(new { error = "AccessDenied" }, JsonRequestBehavior.AllowGet);
            }

            // Common filters
            if (facultyId.HasValue) q = q.Where(a => a.FacultyID == facultyId.Value);
            if (departmentId.HasValue) q = q.Where(a => a.DepartmentID == departmentId.Value);
            if (yearId.HasValue) q = q.Where(a => a.AcademicYearID == yearId.Value);
            if (semesterId.HasValue) q = q.Where(a => a.SemesterID == semesterId.Value);
            if (studentId.HasValue) q = q.Where(a => a.StudentID == studentId.Value);
            if (courseId.HasValue) q = q.Where(a => a.CourseID == courseId.Value);

            var list = q.Select(a => new ReportItem
            {
                StudentName = a.Student.FirstName + " " +
                                 a.Student.MiddleName + " " +
                                 a.Student.LastName,
                CourseName = a.Cours.CourseName,
                FacultyName = a.Faculty.FacultyName,
                DepartmentName = a.Department.DepartmentName,
                YearName = a.AcademicYear.YearName,
                SemesterName = a.Semester.SemesterName,
                FinalStatus = a.FinalStatus
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // GET /Reports/GetStudents
        [HttpGet]
        public JsonResult GetStudents(int facultyId, int departmentId, int yearId, int semesterId)
        {
            var roleName = Session["RoleName"] as string;
            if (roleName != "Staff" && roleName != "Admin")
                return Json(new { error = "AccessDenied" }, JsonRequestBehavior.AllowGet);

            var studs = _db.Students
                          .Where(s =>
                              s.FacultyID == facultyId &&
                              s.DepartmentID == departmentId &&
                              s.AcademicYearID == yearId &&
                              s.SemesterID == semesterId
                          )
                          .Select(s => new {
                              s.StudentID,
                              Name = s.FirstName + " " + s.MiddleName + " " + s.LastName
                          })
                          .ToList();

            return Json(studs, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
