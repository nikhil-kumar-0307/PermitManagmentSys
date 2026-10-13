using System;
using System.Linq;
using System.Web.Mvc;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    [Authorize(Roles = AppRoles.Operation)]
    public class OperationController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly QmsDbContext _qms = new QmsDbContext();   // departments live in the QMS database

        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // GET: /Operation/Dashboard
        public ActionResult Dashboard()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var names = _qms.GetDepartmentNames();

            var model = new OperationDashboardDto
            {
                TotalPermits = _db.Permits.Count(),
                IssuedToday = _db.Permits.Count(p => p.IssueDate >= today && p.IssueDate < tomorrow),
                IssuedThisMonth = _db.Permits.Count(p => p.IssueDate >= monthStart),
                WithoutAttachment = _db.Permits.Count(p => p.AttachmentStoredName == null),

                Recent = _db.Permits
                    .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                    .Take(6)
                    .Select(p => new PermitListDto
                    {
                        Id = p.Id,
                        PermitNo = p.PermitNo,
                        WorkLocation = p.WorkLocation,
                        DepartmentId = p.DepartmentId,
                        PermitIssueTo = p.PermitIssueTo,
                        IssueDate = p.IssueDate,
                        HasAttachment = p.AttachmentStoredName != null
                    }).ToList(),

                ByDepartment = _db.Permits
                    .GroupBy(p => p.DepartmentId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList()
                    .Select(x => new DepartmentCountDto { Name = QmsDbContext.NameOf(names, x.Id), Count = x.Count })
                    .ToList()
            };

            foreach (var r in model.Recent)
                r.Department = QmsDbContext.NameOf(names, r.DepartmentId);

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); _qms.Dispose(); }
            base.Dispose(disposing);
        }
    }
}