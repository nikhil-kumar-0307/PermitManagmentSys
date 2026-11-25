using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Filters;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    [RoleAuthorize(Roles = AppRoles.Operation)]
    public class OperationController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly QmsDbContext _qms = new QmsDbContext();   // departments live in the QMS database

        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        private IQueryable<ClosureRowDto> ToClosureRows(IQueryable<PermitTask> tasks)
        {
            return tasks.Select(t => new ClosureRowDto
            {
                TaskId = t.Id,
                PermitId = t.PermitId,
                PermitNo = t.Permit.PermitNo,
                DepartmentId = t.Permit.DepartmentId,
                WorkLocation = t.Permit.WorkLocation,
                SupervisorName = t.SupervisorName,
                SafetyStewardName = t.SafetyStewardName,
                SafetyStewardMobile = t.SafetyStewardMobile,
                ClosedOn = t.ClosedOn,
                FinalClosedOn = t.FinalClosedOn
            });
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
                AwaitingFinalClose = _db.PermitTasks.Count(t => t.ClosedOn != null && t.FinalClosedOn == null),
                FinalClosed = _db.PermitTasks.Count(t => t.FinalClosedOn != null),

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

                FinalCloseList = ToClosureRows(_db.PermitTasks
                        .Where(t => t.ClosedOn != null && t.FinalClosedOn == null)
                        .OrderByDescending(t => t.ClosedOn).ThenByDescending(t => t.Id)
                        .Take(10))
                    .ToList(),

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
            foreach (var r in model.FinalCloseList)
                r.DepartmentName = QmsDbContext.NameOf(names, r.DepartmentId);

            // Permits per day, last 30 days (days with no permits are included as 0)
            var from = today.AddDays(-29);
            var perDay = _db.Permits
                .Where(p => p.IssueDate >= from && p.IssueDate < tomorrow)
                .GroupBy(p => p.IssueDate)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList();

            model.LastDays = Enumerable.Range(0, 30)
                .Select(i => from.AddDays(i))
                .Select(d => new DayCountDto { Date = d, Count = perDay.Where(x => x.Date == d).Sum(x => x.Count) })
                .ToList();

            return View(model);
        }

        // GET: /Operation/Closures?q=...&status=closing|closed
        // Jobs the departments have closed. Operation does the final close here.
        public ActionResult Closures(string q, string status)
        {
            var query = _db.PermitTasks.Where(t => t.ClosedOn != null);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(t => t.Permit.PermitNo.Contains(q) || t.Permit.WorkLocation.Contains(q)
                                      || t.SupervisorName.Contains(q) || t.SafetyStewardName.Contains(q));
            }

            if (status == "closing") query = query.Where(t => t.FinalClosedOn == null);
            else if (status == "closed") query = query.Where(t => t.FinalClosedOn != null);

            var items = ToClosureRows(query.OrderByDescending(t => t.ClosedOn).ThenByDescending(t => t.Id).Take(200)).ToList();

            var names = _qms.GetDepartmentNames();
            foreach (var r in items) r.DepartmentName = QmsDbContext.NameOf(names, r.DepartmentId);

            return View(new ClosureListDto { Items = items, Query = q, Status = status });
        }

        // POST: /Operation/FinalClose - only after the department has closed the job
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult FinalClose(int taskId, string returnTo, string q, string status)
        {
            var task = _db.PermitTasks.Include(t => t.Permit).FirstOrDefault(t => t.Id == taskId);
            if (task == null) return HttpNotFound();

            if (task.FinalClosedOn != null)
                TempData["Error"] = "Permit " + task.Permit.PermitNo + " is already final closed.";
            else if (task.ClosedOn == null)
                TempData["Error"] = "The department has not closed permit " + task.Permit.PermitNo + " yet.";
            else
            {
                task.FinalClosedOn = DateTime.Now;
                task.FinalClosedBy = User.Identity.Name;
                _db.SaveChanges();
                TempData["Flash"] = "Permit " + task.Permit.PermitNo + " is final closed.";
            }

            if (returnTo == "closures")
                return RedirectToAction("Closures", new { q = q, status = status });
            return RedirectToAction("Dashboard");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); _qms.Dispose(); }
            base.Dispose(disposing);
        }
    }
}