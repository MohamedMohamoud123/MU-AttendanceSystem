using System.Linq;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        // STUDENT DASHBOARD (summary cards + absent percent chart)
        public ActionResult StudentDash()
        {
            if (Session["UserID"] == null || (Session["RoleName"] as string) != "Student")
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];
            var student = db.Students.FirstOrDefault(s => s.UserID == userId);
            if (student == null) return HttpNotFound("Student not found");

            int coursesEnrolled = db.StudentCourses.Count(sc => sc.StudentID == student.StudentID);
            int attendanceRecords = db.Attendances.Count(a => a.StudentID == student.StudentID);
            int totalAllowedCourses = db.Attendances.Count(a => a.StudentID == student.StudentID && a.FinalStatus == "Allowed");
            int totalNotAllowedCourses = db.Attendances.Count(a => a.StudentID == student.StudentID && a.FinalStatus == "Not Allowed");

            var model = new StudentDashboardViewModel
            {
                CoursesEnrolled = coursesEnrolled,
                AttendanceRecords = attendanceRecords,
                TotalAllowedCourses = totalAllowedCourses,
                TotalNotAllowedCourses = totalNotAllowedCourses
            };

            ViewBag.StudentID = student.StudentID;
            return View(model);
        }

        // AJAX: Course-wise Proportion of Total Absent Hours (null-safe)
        [HttpGet]
        public JsonResult GetAllCoursesAbsentPercentByTotal(int studentId)
        {
            var courses = db.StudentCourses
                .Where(sc => sc.StudentID == studentId)
                .Select(sc => sc.Cours)
                .Distinct()
                .ToList();

            var absentList = courses.Select(course => new
            {
                CourseName = course.CourseName,
                CourseAbsent = db.Attendances
                    .Where(a => a.StudentID == studentId && a.CourseID == course.CourseID)
                    .Select(a => (a.HoursAbsent ?? 0))
                    .DefaultIfEmpty(0)
                    .Sum()
            }).ToList();

            int totalAbsent = absentList.Sum(x => x.CourseAbsent);

            var data = absentList.Select(x => new
            {
                x.CourseName,
                PercentOfTotalAbsent = (totalAbsent > 0) ? ((double)x.CourseAbsent * 100.0 / totalAbsent) : 0,
                HoursAbsent = x.CourseAbsent
            }).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // STAFF DASHBOARD (summary)
        public ActionResult StaffDash()
        {
            if (Session["UserID"] == null || (Session["RoleName"] as string) != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpGet]
        public JsonResult GetDashboardCounts()
        {
            var counts = new
            {
                courses = db.Courses.Count(),
                students = db.Students.Count(),
                teachers = db.Teachers.Count(),
                staff = db.Staffs.Count()
            };
            return Json(counts, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetFacultyDepartmentChart()
        {
            var data = db.Faculties
                        .Select(f => new
                        {
                            label = f.FacultyName,
                            faculties = 1,
                            departments = f.Departments.Count()
                        })
                        .ToList();

            return Json(new
            {
                labels = data.Select(x => x.label).ToArray(),
                faculties = data.Select(x => x.faculties).ToArray(),
                departments = data.Select(x => x.departments).ToArray()
            }, JsonRequestBehavior.AllowGet);
        }

        // TEACHER DASHBOARD (final, correct logic)
        public ActionResult TeacherDash()
        {
            if (Session["UserID"] == null || (Session["RoleName"] as string) != "Teacher")
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];
            var teacher = db.Teachers.FirstOrDefault(t => t.UserID == userId);
            if (teacher == null) return HttpNotFound("Teacher not found");

            int teacherId = teacher.TeacherID;
            int coursesAssigned = db.TeacherCourses.Count(tc => tc.TeacherID == teacherId);

            // Students Assigned = all student+course pairs
            int studentsAssigned = db.Attendances
                .Where(a => a.TeacherID == userId)
                .Select(a => new { a.StudentID, a.CourseID })
                .Distinct()
                .Count();

            // Get the latest status per (StudentID, CourseID) using CreatedDate
            var latestStatuses = db.Attendances
                .Where(a => a.TeacherID == userId)
                .GroupBy(a => new { a.StudentID, a.CourseID })
                .Select(g => g.OrderByDescending(a => a.CreatedDate).FirstOrDefault())
                .ToList();

            // Allowed per (student, course)
            int totalAllowed = latestStatuses.Count(a => a.FinalStatus == "Allowed");

            // Not Allowed per (student, course)
            int totalNotAllowed = latestStatuses.Count(a => a.FinalStatus == "Not Allowed");

            // Pie chart
            int totalHoursPresent = db.Attendances.Where(a => a.TeacherID == userId).Sum(a => (int?)a.HoursPresent) ?? 0;
            int totalHoursAbsent = db.Attendances.Where(a => a.TeacherID == userId).Sum(a => (int?)a.HoursAbsent) ?? 0;

            ViewBag.TotalAllowedStudents = totalAllowed;
            ViewBag.TotalNotAllowedStudents = totalNotAllowed;
            ViewBag.TotalHoursPresent = totalHoursPresent;
            ViewBag.TotalHoursAbsent = totalHoursAbsent;

            var model = new TeacherDashboardViewModel
            {
                CoursesAssigned = coursesAssigned,
                StudentsAssigned = studentsAssigned
            };

            ViewBag.TeacherID = teacher.TeacherID;
            return View(model);
        }

        [HttpGet]
        public JsonResult GetTeacherCalendarEvents(int teacherId)
        {
            var events = db.TeacherCourses
                .Where(tc => tc.TeacherID == teacherId)
                .Select(tc => new
                {
                    title = tc.Cours.CourseName,
                    start = System.Data.Entity.SqlServer.SqlFunctions.DateAdd("day", tc.CourseID, System.DateTime.Today),
                    end = System.Data.Entity.SqlServer.SqlFunctions.DateAdd("day", tc.CourseID, System.DateTime.Today).Value.AddHours(2),
                    allDay = false
                })
                .ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
