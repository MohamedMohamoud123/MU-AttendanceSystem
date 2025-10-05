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
    public class TeachersController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ───────── helpers ───────── */
        private bool IsStudent() => (Session["RoleID"] as int?) == 3;
        private bool IsTeacher() => (Session["RoleID"] as int?) == 2;
        private bool IsAdmin() => (Session["RoleID"] as int?) == 1;
        private ActionResult Denied() => RedirectToAction("AccessDenied", "Account");

        // ───────── GET: Teachers ─────────
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin())
                return Denied();

            var teachers = string.IsNullOrWhiteSpace(searchQuery)
                ? db.Teachers.ToList()
                : db.Teachers.Where(t =>
                    t.TeacherRollID.Contains(searchQuery) ||
                    (t.FirstName + " " + t.MiddleName + " " + t.LastName)
                        .Contains(searchQuery))
                  .ToList();

            return View(teachers);
        }

        // ───────── GET: Teachers/Details/5 ─────────
        public ActionResult Details(int? id)
        {
            // Teachers may view only their own record (whether or not they supply an id)
            if (IsTeacher())
            {
                int uid = Convert.ToInt32(Session["UserID"]);
                var me = db.Teachers.FirstOrDefault(t => t.UserID == uid);
                if (me == null) return Denied();

                // if no id or id matches their own TeacherID, show their detail
                if (id == null || id.Value == me.TeacherID)
                {
                    ViewBag.AssignedCourses = db.TeacherCourses
                        .Where(tc => tc.TeacherID == me.TeacherID)
                        .Select(tc => tc.Cours.CourseName)
                        .ToList();
                    return View(me);
                }

                // otherwise block
                return Denied();
            }

            // Admin may view any teacher by id
            if (IsAdmin())
            {
                if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                var teacher = db.Teachers.Find(id.Value);
                if (teacher == null) return HttpNotFound();

                ViewBag.AssignedCourses = db.TeacherCourses
                    .Where(tc => tc.TeacherID == teacher.TeacherID)
                    .Select(tc => tc.Cours.CourseName)
                    .ToList();
                return View(teacher);
            }

            // Students and others denied
            return Denied();
        }

        // ───────── GET: Teachers/Create ─────────
        public ActionResult Create()
        {
            if (!IsAdmin())
                return Denied();

            ViewBag.Roles = new SelectList(db.Roles, "RoleID", "RoleName");
            return View();
        }

        // ───────── POST: Teachers/Create ─────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include =
            "TeacherID,TeacherRollID,FirstName,MiddleName,LastName,CreatedDate,User")]
            Teacher teacher)
        {
            if (!IsAdmin())
                return Denied();

            ViewBag.Roles = new SelectList(db.Roles, "RoleID", "RoleName", teacher.User?.RoleID);

            if (db.Teachers.Any(t => t.TeacherRollID == teacher.TeacherRollID))
                ModelState.AddModelError(nameof(teacher.TeacherRollID), "This Roll ID has already been used.");

            if (!ModelState.IsValid)
                return View(teacher);

            if (teacher.User == null)
                teacher.User = new User();

            teacher.User.FullName = $"{teacher.FirstName} {teacher.MiddleName} {teacher.LastName}";
            teacher.User.UserName = teacher.TeacherRollID;
            teacher.User.Password = "123";
            teacher.User.IsFirstLogin = true;
            teacher.User.RoleID = 2;

            db.Users.Add(teacher.User);
            db.SaveChanges();

            teacher.UserID = teacher.User.UserID;
            db.Teachers.Add(teacher);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // ───────── GET: Teachers/Edit/5 ─────────
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin())
                return Denied();

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var teacher = db.Teachers.Find(id.Value);
            if (teacher == null) return HttpNotFound();

            ViewBag.UserID = new SelectList(db.Users, "UserID", "UserName", teacher.UserID);
            return View(teacher);
        }

        // ───────── POST: Teachers/Edit/5 ─────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include =
            "TeacherID,UserID,TeacherRollID,FirstName,MiddleName,LastName,CreatedDate")]
            Teacher teacher)
        {
            if (!IsAdmin())
                return Denied();

            ViewBag.UserID = new SelectList(db.Users, "UserID", "UserName", teacher.UserID);

            if (db.Teachers.Any(t =>
                    t.TeacherRollID == teacher.TeacherRollID &&
                    t.TeacherID != teacher.TeacherID))
            {
                ModelState.AddModelError(nameof(teacher.TeacherRollID), "This Roll ID has already been used.");
            }

            if (!ModelState.IsValid)
                return View(teacher);

            db.Entry(teacher).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ───────── GET: Teachers/Delete/5 ─────────
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin())
                return Denied();

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var teacher = db.Teachers.Find(id.Value);
            if (teacher == null) return HttpNotFound();

            return View(teacher);
        }

        // ───────── POST: Teachers/Delete/5 ─────────
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin())
                return Denied();

            var teacher = db.Teachers.Find(id);
            if (teacher != null)
            {
                db.Teachers.Remove(teacher);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
