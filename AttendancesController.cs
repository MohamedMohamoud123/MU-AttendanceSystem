using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;

namespace MU_AttendanceSystem.Controllers
{
    public class AttendanceController : BaseController
    {
        private readonly MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────────── role checks & redirect ─────────────── */
        private bool IsStudent()
            => String.Equals(Session["RoleName"] as string, "Student", StringComparison.OrdinalIgnoreCase);

        private bool IsTeacher()
            => String.Equals(Session["RoleName"] as string, "Teacher", StringComparison.OrdinalIgnoreCase);

        private bool IsAdmin()
            => String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        /* ─────────────── View‐model (unchanged) ─────────────── */
        public class AttendanceBatchItem
        {
            public int AttendanceID { get; set; }
            public string CourseName { get; set; }
            public string TeacherName { get; set; }
            public string FacultyName { get; set; }
            public string DepartmentName { get; set; }
            public string YearName { get; set; }
            public string SemesterName { get; set; }
            public DateTime StartMonth { get; set; }
            public DateTime EndMonth { get; set; }
        }

        // ─────────────── INDEX ───────────────
        public ActionResult Index()
        {
            if (IsStudent()) return Denied();

            var q =
                from a in db.Attendances
                join c in db.Courses on a.CourseID equals c.CourseID
                join t in db.Teachers on a.TeacherID equals t.UserID
                join f in db.Faculties on a.FacultyID equals f.FacultyID
                join d in db.Departments on a.DepartmentID equals d.DepartmentID
                join y in db.AcademicYears on a.AcademicYearID equals y.AcademicYearID
                join s in db.Semesters on a.SemesterID equals s.SemesterID
                select new { a, c, t, f, d, y, s };

            if (IsTeacher() && Session["UserID"] is int uid)
                q = q.Where(x => x.a.TeacherID == uid);

            var batches = q
                .GroupBy(g => new
                {
                    g.a.CourseID,
                    g.c.CourseName,
                    g.a.TeacherID,
                    g.t.FirstName,
                    g.t.MiddleName,
                    g.t.LastName,
                    g.a.FacultyID,
                    g.f.FacultyName,
                    g.a.DepartmentID,
                    g.d.DepartmentName,
                    g.a.AcademicYearID,
                    g.y.YearName,
                    g.a.SemesterID,
                    g.s.SemesterName,
                    g.a.StartMonth,
                    g.a.EndMonth
                })
                .Select(g => new AttendanceBatchItem
                {
                    AttendanceID = g.Min(x => x.a.AttendanceID),
                    CourseName = g.Key.CourseName,
                    TeacherName = g.Key.FirstName + " " + g.Key.MiddleName + " " + g.Key.LastName,
                    FacultyName = g.Key.FacultyName,
                    DepartmentName = g.Key.DepartmentName,
                    YearName = g.Key.YearName,
                    SemesterName = g.Key.SemesterName,
                    StartMonth = g.Key.StartMonth,
                    EndMonth = g.Key.EndMonth
                })
                .ToList();

            ViewBag.IsTeacher = IsTeacher();
            return View(batches);
        }

        // ─────────────── dropdown helper (unchanged) ───────────────
        private void PopulateDropdowns(int? facultyId = null, int? yearId = null)
        {
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", facultyId);
            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", yearId);

            ViewBag.DepartmentID = facultyId.HasValue
                ? new SelectList(db.Departments.Where(d => d.FacultyID == facultyId.Value), "DepartmentID", "DepartmentName")
                : new SelectList(Enumerable.Empty<Department>(), "DepartmentID", "DepartmentName");

            ViewBag.SemesterID = yearId.HasValue
                ? new SelectList(db.Semesters.Where(s => s.AcademicYearID == yearId.Value), "SemesterID", "SemesterName")
                : new SelectList(Enumerable.Empty<Semester>(), "SemesterID", "SemesterName");

            ViewBag.CourseID = new SelectList(Enumerable.Empty<Cours>(), "CourseID", "CourseName");
            ViewBag.TeacherID = new SelectList(Enumerable.Empty<Teacher>(), "TeacherID", "FirstName");
        }

        // ─────────────── CREATE ───────────────
        [HttpGet]
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();
            PopulateDropdowns();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(int FacultyID, int DepartmentID, int AcademicYearID, int SemesterID,
                                   int CourseID, int TeacherID, DateTime StartMonth, DateTime EndMonth)
        {
            if (!IsAdmin()) return Denied();

           
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(FacultyID, AcademicYearID);
                return View();
            }

            bool exists = db.Attendances.Any(a =>
                a.FacultyID == FacultyID &&
                a.DepartmentID == DepartmentID &&
                a.SemesterID == SemesterID &&
                a.CourseID == CourseID);

            if (exists)
            {
                ModelState.AddModelError("", "Attendance for this course already exists. Please select a different course.");
                PopulateDropdowns(FacultyID, AcademicYearID);
                return View();
            }

            var teacher = db.Teachers.FirstOrDefault(x => x.TeacherID == TeacherID);
            if (teacher == null)
            {
                ModelState.AddModelError("", "Selected Teacher does not exist.");
                PopulateDropdowns(FacultyID, AcademicYearID);
                return View();
            }
            int teacherUserID = teacher.UserID;

            var students = db.Students.Where(s =>
                s.FacultyID == FacultyID &&
                s.DepartmentID == DepartmentID &&
                s.AcademicYearID == AcademicYearID &&
                s.SemesterID == SemesterID).ToList();

            foreach (var st in students)
            {
                db.Attendances.Add(new Attendance
                {
                    StudentID = st.StudentID,
                    TeacherID = teacherUserID,
                    CourseID = CourseID,
                    FacultyID = FacultyID,
                    DepartmentID = DepartmentID,
                    SemesterID = SemesterID,
                    AcademicYearID = AcademicYearID,
                    StartMonth = StartMonth,
                    EndMonth = EndMonth,
                    HoursPresent = 0,
                    HoursAbsent = 0,
                    FinalStatus = "Allowed",
                    CreatedDate = DateTime.Now
                });
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ─────────────── CREATE AJAX ───────────────
        [HttpPost]
        public JsonResult CreateAjax(int FacultyID, int DepartmentID, int AcademicYearID, int SemesterID,
                                     int CourseID, int TeacherID, DateTime StartMonth, DateTime EndMonth)
        {
            if (!IsAdmin())
                return Json(new { success = false, message = "Access denied." });

            bool exists = db.Attendances.Any(a =>
                a.FacultyID == FacultyID &&
                a.DepartmentID == DepartmentID &&
                a.SemesterID == SemesterID &&
                a.CourseID == CourseID);

            if (exists)
                return Json(new { success = false, message = "Attendance for this course already exists. Please select a different course." });

            var teacher = db.Teachers.FirstOrDefault(x => x.TeacherID == TeacherID);
            if (teacher == null)
                return Json(new { success = false, message = "Selected Teacher does not exist." });

            int teacherUserID = teacher.UserID;
            var students = db.Students.Where(s =>
                s.FacultyID == FacultyID &&
                s.DepartmentID == DepartmentID &&
                s.AcademicYearID == AcademicYearID &&
                s.SemesterID == SemesterID).ToList();

            foreach (var st in students)
            {
                db.Attendances.Add(new Attendance
                {
                    StudentID = st.StudentID,
                    TeacherID = teacherUserID,
                    CourseID = CourseID,
                    FacultyID = FacultyID,
                    DepartmentID = DepartmentID,
                    SemesterID = SemesterID,
                    AcademicYearID = AcademicYearID,
                    StartMonth = StartMonth,
                    EndMonth = EndMonth,
                    HoursPresent = 0,
                    HoursAbsent = 0,
                    FinalStatus = "Allowed",
                    CreatedDate = DateTime.Now
                });
            }
            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────── EDIT ───────────────
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var att = db.Attendances.Find(id);
            if (att == null) return HttpNotFound();

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", att.FacultyID);
            ViewBag.AcademicYearID = new SelectList(db.AcademicYears, "AcademicYearID", "YearName", att.AcademicYearID);
            ViewBag.DepartmentID = new SelectList(db.Departments.Where(d => d.FacultyID == att.FacultyID), "DepartmentID", "DepartmentName", att.DepartmentID);
            ViewBag.SemesterID = new SelectList(db.Semesters.Where(s => s.AcademicYearID == att.AcademicYearID), "SemesterID", "SemesterName", att.SemesterID);
            ViewBag.CourseID = new SelectList(db.Courses.Where(c =>
                                                c.FacultyID == att.FacultyID &&
                                                c.DepartmentID == att.DepartmentID &&
                                                c.SemesterID == att.SemesterID),
                                                "CourseID", "CourseName", att.CourseID);
            ViewBag.TeacherID = new SelectList(db.Teachers
                                                .Where(tc => db.TeacherCourses.Any(tcc => tcc.TeacherID == tc.TeacherID &&
                                                                                          tcc.CourseID == att.CourseID))
                                                .Distinct(), "TeacherID", "FirstName", att.TeacherID);
            return View(att);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AttendanceID,FacultyID,DepartmentID,AcademicYearID,SemesterID,CourseID,TeacherID,StartMonth,EndMonth")] Attendance attendance)
        {
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(attendance).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            PopulateDropdowns(attendance.FacultyID, attendance.AcademicYearID);
            ViewBag.CourseID = new SelectList(db.Courses.Where(c =>
                                        c.FacultyID == attendance.FacultyID &&
                                        c.DepartmentID == attendance.DepartmentID &&
                                        c.SemesterID == attendance.SemesterID), "CourseID", "CourseName", attendance.CourseID);
            ViewBag.TeacherID = new SelectList(db.Teachers
                                        .Where(tc => db.TeacherCourses.Any(tcc => tcc.TeacherID == tc.TeacherID &&
                                                                                  tcc.CourseID == attendance.CourseID))
                                        .Distinct(), "TeacherID", "FirstName", attendance.TeacherID);

            return View(attendance);
        }

        // ─────────────── DELETE ───────────────
        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var att = db.Attendances.Find(id);
            if (att == null) return HttpNotFound();

            ViewBag.FacultyName = db.Faculties.Find(att.FacultyID)?.FacultyName ?? "";
            ViewBag.DepartmentName = db.Departments.Find(att.DepartmentID)?.DepartmentName ?? "";
            ViewBag.CourseName = db.Courses.Find(att.CourseID)?.CourseName ?? "";
            var tch = db.Teachers.FirstOrDefault(x => x.UserID == att.TeacherID);
            ViewBag.TeacherName = tch != null ? tch.FirstName + " " + tch.MiddleName + " " + tch.LastName : "";
            ViewBag.YearName = db.AcademicYears.Find(att.AcademicYearID)?.YearName ?? "";
            ViewBag.SemesterName = db.Semesters.Find(att.SemesterID)?.SemesterName ?? "";

            return View(att);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            var att = db.Attendances.Find(id);
            if (att == null) return HttpNotFound();

            var batchDate = att.CreatedDate.Value.Date;
            var batch = db.Attendances.Where(a =>
                    DbFunctions.TruncateTime(a.CreatedDate) == batchDate &&
                    a.CourseID == att.CourseID &&
                    a.FacultyID == att.FacultyID &&
                    a.DepartmentID == att.DepartmentID &&
                    a.AcademicYearID == att.AcademicYearID &&
                    a.SemesterID == att.SemesterID &&
                    a.StartMonth == att.StartMonth &&
                    a.EndMonth == att.EndMonth).ToList();

            var ids = batch.Select(a => a.AttendanceID).ToList();
            db.AttendanceDetails.RemoveRange(db.AttendanceDetails.Where(d => ids.Contains(d.AttendanceID)));
            db.Attendances.RemoveRange(batch);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // ─────────────── FILL‐ATTENDANCE ───────────────
        [HttpGet]
        public ActionResult FillAttendance(int? id, string selectedMonth, int? selectedWeek)
        {
            if (IsStudent()) return Denied();

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var template = db.Attendances.Find(id.Value);
            if (template == null) return HttpNotFound();

            if (IsTeacher() && Session["UserID"] is int tid && template.TeacherID != tid)
                return Denied();

            if (!IsTeacher() && !IsAdmin())
                return Denied();

            // (original FillAttendance GET body unchanged)
            DateTime batchDate = template.CreatedDate.Value.Date;
            var batchRecords = db.Attendances
                .Where(a => DbFunctions.TruncateTime(a.CreatedDate) == batchDate &&
                            a.CourseID == template.CourseID &&
                            a.FacultyID == template.FacultyID &&
                            a.DepartmentID == template.DepartmentID &&
                            a.AcademicYearID == template.AcademicYearID &&
                            a.SemesterID == template.SemesterID)
                .ToList();

            var studentIds = batchRecords.Select(a => a.StudentID).ToList();
            var students = db.Students
                                .Where(s => studentIds.Contains(s.StudentID))
                                .OrderBy(s => s.FirstName)
                                .ThenBy(s => s.MiddleName)
                                .ThenBy(s => s.LastName)
                                .ToList();

            var months = new List<string>();
            var cur = new DateTime(template.StartMonth.Year, template.StartMonth.Month, 1);
            while (cur <= template.EndMonth)
            {
                months.Add(cur.ToString("MMMM", CultureInfo.InvariantCulture));
                cur = cur.AddMonths(1);
            }
            if (String.IsNullOrEmpty(selectedMonth)) selectedMonth = months.First();
            if (!selectedWeek.HasValue) selectedWeek = 1;

            ViewBag.Months = months;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedWeek = selectedWeek.Value;
            ViewBag.Students = students;

            int monthNumber = DateTime.ParseExact(selectedMonth, "MMMM", CultureInfo.InvariantCulture).Month;
            var attIds = batchRecords.Select(a => a.AttendanceID).ToList();

            var weekDetails = db.AttendanceDetails
                                .Where(d => attIds.Contains(d.AttendanceID) &&
                                            d.Month == monthNumber &&
                                            d.WeekNumber == selectedWeek.Value)
                                .ToList()
                                .GroupBy(d => d.StudentID)
                                .ToDictionary(g => g.Key, g => g.First());
            ViewBag.WeekDetails = weekDetails;

            var cumulativeResult = new Dictionary<int, string>();
            foreach (var a in batchRecords)
            {
                var dets = db.AttendanceDetails
                             .Where(d => d.AttendanceID == a.AttendanceID)
                             .ToList();
                int pres = dets.Sum(d => (d.Hour1Present ? 1 : 0) +
                                         (d.Hour2Present ? 1 : 0) +
                                         (d.Hour3Present ? 1 : 0));
                int slots = dets.Count * 3;
                cumulativeResult[a.StudentID] = (slots - pres) > 12 ? "Not Allowed" : "Allowed";
            }
            ViewBag.CumulativeResult = cumulativeResult;

            ViewBag.FacultyName = db.Faculties.Find(template.FacultyID)?.FacultyName ?? "";
            ViewBag.DepartmentName = db.Departments.Find(template.DepartmentID)?.DepartmentName ?? "";
            ViewBag.CourseName = db.Courses.Find(template.CourseID)?.CourseName ?? "";
            var tch = db.Teachers.FirstOrDefault(x => x.UserID == template.TeacherID);
            ViewBag.TeacherName = tch != null ? tch.FirstName + " " + tch.MiddleName + " " + tch.LastName : "";
            ViewBag.YearName = db.AcademicYears.Find(template.AcademicYearID)?.YearName ?? "";
            ViewBag.SemesterName = db.Semesters.Find(template.SemesterID)?.SemesterName ?? "";

            return View(template);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult FillAttendance(int id, string selectedMonth, int selectedWeek, FormCollection form)
        {
            if (IsStudent()) return Denied();
            if (!IsTeacher() && !IsAdmin()) return Denied();

            var template = db.Attendances.Find(id);
            if (template == null) return HttpNotFound();

            if (IsTeacher() && Session["UserID"] is int tid && template.TeacherID != tid)
                return Denied();

            // (original FillAttendance POST body unchanged)
            DateTime batchDate = template.CreatedDate.Value.Date;
            var batchRecords = db.Attendances
                .Where(a => DbFunctions.TruncateTime(a.CreatedDate) == batchDate &&
                            a.CourseID == template.CourseID &&
                            a.FacultyID == template.FacultyID &&
                            a.DepartmentID == template.DepartmentID &&
                            a.AcademicYearID == template.AcademicYearID &&
                            a.SemesterID == template.SemesterID)
                .ToList();

            var validStudentIds = new HashSet<int>(db.Students.Select(s => s.StudentID));
            batchRecords = batchRecords.Where(a => validStudentIds.Contains(a.StudentID)).ToList();

            int monthNumber = DateTime.ParseExact(selectedMonth, "MMMM", CultureInfo.InvariantCulture).Month;

            foreach (var att in batchRecords)
            {
                bool h1 = form.AllKeys.Contains($"present_{att.StudentID}_hr1");
                bool h2 = form.AllKeys.Contains($"present_{att.StudentID}_hr2");
                bool h3 = form.AllKeys.Contains($"present_{att.StudentID}_hr3");

                var detail = db.AttendanceDetails.FirstOrDefault(d =>
                    d.AttendanceID == att.AttendanceID &&
                    d.StudentID == att.StudentID &&
                    d.Month == monthNumber &&
                    d.WeekNumber == selectedWeek);

                if (detail == null)
                {
                    detail = new AttendanceDetail
                    {
                        AttendanceID = att.AttendanceID,
                        StudentID = att.StudentID,
                        Month = (byte)monthNumber,
                        WeekNumber = (byte)selectedWeek,
                        Hour1Present = h1,
                        Hour2Present = h2,
                        Hour3Present = h3
                    };
                    db.AttendanceDetails.Add(detail);
                }
                else
                {
                    detail.Hour1Present = h1;
                    detail.Hour2Present = h2;
                    detail.Hour3Present = h3;
                    db.Entry(detail).State = EntityState.Modified;
                }
            }
            db.SaveChanges();

            foreach (var att in batchRecords)
            {
                var dets = db.AttendanceDetails.Where(d => d.AttendanceID == att.AttendanceID).ToList();
                att.HoursPresent = dets.Sum(d => (d.Hour1Present ? 1 : 0) +
                                                 (d.Hour2Present ? 1 : 0) +
                                                 (d.Hour3Present ? 1 : 0));
                int slots = dets.Count * 3;
                att.HoursAbsent = slots - att.HoursPresent;
                att.FinalStatus = att.HoursAbsent > 12 ? "Not Allowed" : "Allowed";
                db.Entry(att).State = EntityState.Modified;
            }
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // ─────────────── MyAttendance ───────────────
        public class StudentCourseAttendanceVM
        {
            public string CourseName { get; set; }
            public int CreditHours { get; set; }
            public int HoursPresent { get; set; }
            public int HoursAbsent { get; set; }
            public string FinalStatus { get; set; }
        }

        [HttpGet]
        public ActionResult MyAttendance()
        {
            if (!IsStudent()) return Denied();

            int? uidNullable = Session["UserID"] as int?;
            if (!uidNullable.HasValue) return RedirectToAction("Login", "Account");
            int userId = uidNullable.Value;

            var stu = db.Students.FirstOrDefault(s => s.UserID == userId);
            if (stu == null) return Denied();
            int studentId = stu.StudentID;

            var list =
                from a in db.Attendances
                join c in db.Courses on a.CourseID equals c.CourseID
                where a.StudentID == studentId
                group new { a, c } by new { c.CourseName, c.CreditHours }
                into g
                select new StudentCourseAttendanceVM
                {
                    CourseName = g.Key.CourseName,
                    CreditHours = g.Key.CreditHours,
                    HoursPresent = (g.Sum(x => (int?)x.a.HoursPresent) ?? 0),
                    HoursAbsent = (g.Sum(x => (int?)x.a.HoursAbsent) ?? 0),
                    FinalStatus = g.Max(x => x.a.FinalStatus)
                };

            return View(list.ToList());
        }

        // ───────────── AJAX helpers & Dispose ─────────────
        [HttpGet]
        public JsonResult GetDepartments(int facultyId) =>
            Json(db.Departments.Where(d => d.FacultyID == facultyId)
                               .Select(d => new { d.DepartmentID, d.DepartmentName })
                               .ToList(), JsonRequestBehavior.AllowGet);

        [HttpGet]
        public JsonResult GetSemesters(int academicYearId) =>
            Json(db.Semesters.Where(s => s.AcademicYearID == academicYearId)
                             .Select(s => new { s.SemesterID, s.SemesterName })
                             .ToList(), JsonRequestBehavior.AllowGet);

        [HttpGet]
        public JsonResult GetCourses(int facultyId, int departmentId, int semesterId) =>
            Json(db.Courses.Where(c =>
                    c.FacultyID == facultyId &&
                    c.DepartmentID == departmentId &&
                    c.SemesterID == semesterId)
                .Select(c => new { c.CourseID, c.CourseName })
                .ToList(), JsonRequestBehavior.AllowGet);

        [HttpGet]
        public JsonResult GetTeachers(int courseId) =>
            Json(db.TeacherCourses.Where(tc => tc.CourseID == courseId)
                                   .Select(tc => new {
                                       tc.TeacherID,
                                       Name = tc.Teacher.FirstName + " " + tc.Teacher.MiddleName + " " + tc.Teacher.LastName
                                   })
                                   .Distinct()
                                   .ToList(), JsonRequestBehavior.AllowGet);

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
