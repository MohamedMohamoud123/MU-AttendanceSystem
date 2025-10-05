using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MU_AttendanceSystem.Models;
using MU_AttendanceSystem.Validation;

namespace MU_AttendanceSystem.Controllers
{
    public class StaffsController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────── role check & redirect ─────────── */
        private bool IsAdmin()
            => String.Equals(Session["RoleName"] as string, "Admin", StringComparison.OrdinalIgnoreCase);

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        // GET: Staffs
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            if (string.IsNullOrEmpty(searchQuery))
                return View(db.Staffs.ToList());

            var filteredStaff = db.Staffs
                .Where(s =>
                    s.StaffRollID.ToString().Contains(searchQuery) ||
                    (s.FirstName + " " + s.MiddleName + " " + s.LastName).Contains(searchQuery)
                )
                .ToList();

            return View(filteredStaff);
        }

        // GET: Staffs/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var staff = db.Staffs.Find(id.Value);
            if (staff == null) return HttpNotFound();

            return View(staff);
        }

        // GET: Staffs/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();
            ViewBag.Roles = new SelectList(db.Roles, "RoleID", "RoleName");
            return View();
        }

        // POST: Staffs/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "StaffID,StaffRollID,FirstName,MiddleName,LastName,CreatedDate")] StaffDto model)
        {
            if (!IsAdmin()) return Denied();

            var staff = new Staff()
            {
                StaffID = model.StaffID,
                StaffRollID = model.StaffRollID,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                CreatedDate = model.CreatedDate,
            };
            ViewBag.Roles = new SelectList(db.Roles, "RoleID", "RoleName", staff.User?.RoleID);

            if (db.Staffs.Any(s => s.StaffRollID == staff.StaffRollID))
                ModelState.AddModelError(nameof(staff.StaffRollID), "This Roll ID has already been used.");

            if (ModelState.IsValid)
            {
                if (staff.User == null) staff.User = new User();
                staff.User.UserName = staff.StaffRollID.ToString();
                staff.User.Password = "123";
                staff.User.IsFirstLogin = true;
                staff.User.RoleID = 1;
                staff.User.FullName = $"{staff.FirstName} {staff.MiddleName} {staff.LastName}";
                if (staff.User.CreatedDate == DateTime.MinValue)
                    staff.User.CreatedDate = DateTime.Now;

                db.Users.Add(staff.User);
                db.SaveChanges();

                staff.UserID = staff.User.UserID;
                if (staff.CreatedDate == DateTime.MinValue)
                    staff.CreatedDate = DateTime.Now;

                db.Staffs.Add(staff);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(staff);
        }

        // GET: Staffs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var staff = db.Staffs.Find(id.Value);
            if (staff == null) return HttpNotFound();

            ViewBag.UserID = new SelectList(db.Users, "UserID", "UserName", staff.UserID);
            return View(staff);
        }

        // POST: Staffs/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "StaffID,UserID,StaffRollID,FirstName,MiddleName,LastName,CreatedDate")] StaffDto model)
        {
            if (!IsAdmin()) return Denied();

            var staff = new Staff()
            {
                StaffID = model.StaffID,
                StaffRollID = model.StaffRollID,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                CreatedDate = model.CreatedDate,
            };
            ViewBag.UserID = new SelectList(db.Users, "UserID", "UserName", staff.UserID);

            if (db.Staffs.Any(s => s.StaffRollID == staff.StaffRollID && s.StaffID != staff.StaffID))
                ModelState.AddModelError(nameof(staff.StaffRollID), "This Roll ID has already been used.");

            if (ModelState.IsValid)
            {
                if (staff.CreatedDate == DateTime.MinValue)
                    staff.CreatedDate = DateTime.Now;

                db.Entry(staff).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(staff);
        }

        // GET: Staffs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var staff = db.Staffs.Find(id.Value);
            if (staff == null) return HttpNotFound();

            return View(staff);
        }

        // POST: Staffs/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            var staff = db.Staffs.Find(id);
            db.Staffs.Remove(staff);
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
