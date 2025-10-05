// Models/ReportItem.cs
using System;

namespace MU_AttendanceSystem.Models
{
    /// <summary>
    /// View-Model loo isticmaalo in laga soo qaato Attendances
    /// kuwa leh FinalStatus = "Not Allowed" kadibna loo soo bandhigo.
    /// </summary>
    public class ReportItem
    {
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public string FacultyName { get; set; }
        public string DepartmentName { get; set; }
        public string YearName { get; set; }
        public string SemesterName { get; set; }
        public string FinalStatus { get; set; }
    }
}
