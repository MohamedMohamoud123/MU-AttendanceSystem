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
    public class DepartmentsController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ─────────── role check & redirect ─────────── */
        private bool IsAdmin()
            => (Session["RoleName"] as string)?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

        private ActionResult Denied()
            => RedirectToAction("AccessDenied", "Account");

        // GET: Departments
        public ActionResult Index(string searchQuery)
        {
            if (!IsAdmin()) return Denied();

            if (string.IsNullOrEmpty(searchQuery))
            {
                return View(db.Departments.ToList());
            }

            var filteredDepartments = db.Departments
                .Where(f => f.DepartmentName.Contains(searchQuery))
                .ToList();

            return View(filteredDepartments);
        }

        // GET: Departments/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return Denied();

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName");
            return View();
        }

        // POST: Departments/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DepartmentID,DepartmentName,FacultyID,CreatedDate")] DepartmentDto model)
        {
            var department = new Department()
            {
                DepartmentID = model.DepartmentID,
                DepartmentName = model.DepartmentName,
                FacultyID = model.FacultyID,
                CreatedDate = DateTime.Now,
            };
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Departments.Add(department);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", department.FacultyID);
            return View(department);
        }

        // GET: Departments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Denied();

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Department department = db.Departments.Find(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", department.FacultyID);
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DepartmentID,DepartmentName,FacultyID,CreatedDate")] DepartmentDto model)
        {
            var department = new Department()
            {
                DepartmentID = model.DepartmentID,
                DepartmentName = model.DepartmentName,
                FacultyID = model.FacultyID,
                CreatedDate = DateTime.Now,
            };
            if (!IsAdmin()) return Denied();

            if (ModelState.IsValid)
            {
                db.Entry(department).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.FacultyID = new SelectList(db.Faculties, "FacultyID", "FacultyName", department.FacultyID);
            return View(department);
        }

        // GET: Departments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Denied();

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Department department = db.Departments.Find(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return Denied();

            Department department = db.Departments.Find(id);
            db.Departments.Remove(department);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
