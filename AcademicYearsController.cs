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
    public class AcademicYearsController : BaseController
    {
        private MU_AttendanceSystemDBEntities db = new MU_AttendanceSystemDBEntities();

        /* ───────── helpers ───────── */
        private bool IsAdmin()
        {
            /* adjust if your role logic differs:
               – Session["RoleName"] == "Admin"
               – OR RoleID == 1                                        */
            var roleName = Session["RoleName"] as string;
            if (roleName != null && roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            return (Session["RoleID"] as int?) == 1;
        }

        private ActionResult Denied() =>
            RedirectToAction("AccessDenied", "Account");

        /* apply the guard to EVERY action in this controller */
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = Denied();
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // ───────────────────────────── GET: AcademicYears
        public ActionResult Index(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
                return View(db.AcademicYears.ToList());

            var filtered = db.AcademicYears
                             .Where(f => f.YearName.Contains(searchQuery))
                             .ToList();

            return View(filtered);
        }

        // ───────────────────────────── GET: AcademicYears/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            AcademicYear academicYear = db.AcademicYears.Find(id);
            if (academicYear == null) return HttpNotFound();
            return View(academicYear);
        }

        // ───────────────────────────── GET: AcademicYears/Create
        public ActionResult Create() => View();

        // ───────────────────────────── POST: AcademicYears/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AcademicYearID,YearName,CreatedDate")] AcademicYearDto model)
        {
            var academicYear = new AcademicYear()
            {
                AcademicYearID = model.AcademicYearID,
                YearName = model.YearName,
                CreatedDate = DateTime.Now,
            };
            if (ModelState.IsValid)
            {
                db.AcademicYears.Add(academicYear);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(academicYear);
        }

        // ───────────────────────────── GET: AcademicYears/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            AcademicYear academicYear = db.AcademicYears.Find(id);
            if (academicYear == null) return HttpNotFound();
            return View(academicYear);
        }

        // ───────────────────────────── POST: AcademicYears/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AcademicYearID,YearName,CreatedDate")] AcademicYearDto model)
        {
            var academicYear = new AcademicYear()
            {
                AcademicYearID = model.AcademicYearID,
                YearName = model.YearName,
                CreatedDate = DateTime.Now,
            };

            if (ModelState.IsValid)
            {
                db.Entry(academicYear).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(academicYear);
        }

        // ───────────────────────────── GET: AcademicYears/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            AcademicYear academicYear = db.AcademicYears.Find(id);
            if (academicYear == null) return HttpNotFound();
            return View(academicYear);
        }

        // ───────────────────────────── POST: AcademicYears/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AcademicYear academicYear = db.AcademicYears.Find(id);
            db.AcademicYears.Remove(academicYear);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ───────────────────────────── cleanup
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
