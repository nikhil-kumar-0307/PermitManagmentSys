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
                        Department = p.Department,
                        PermitIssueTo = p.PermitIssueTo,
                        IssueDate = p.IssueDate,
                        HasAttachment = p.AttachmentStoredName != null
                    }).ToList(),

                ByDepartment = _db.Permits
                    .GroupBy(p => p.Department)
                    .Select(g => new DepartmentCountDto { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}