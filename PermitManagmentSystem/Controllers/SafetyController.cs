using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    // Safety sees the work saved by EVERY department and records the Safety Steward.
    [Authorize(Roles = AppRoles.Safety)]
    public class SafetyController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly QmsDbContext _qms = new QmsDbContext();

        // Same folder the Operation side saves attachments in - change if yours is different
        private const string AttachmentFolder = "~/App_Data/Permits/";

        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        private IQueryable<SafetyTaskRowDto> ToRows(IQueryable<PermitTask> tasks)
        {
            return tasks.Select(t => new SafetyTaskRowDto
            {
                TaskId = t.Id,
                PermitId = t.PermitId,
                PermitNo = t.Permit.PermitNo,
                DepartmentId = t.Permit.DepartmentId,
                WorkLocation = t.Permit.WorkLocation,
                SupervisorName = t.SupervisorName,
                WorkStartDate = t.WorkStartDate,
                ExpectedEndDate = t.ExpectedEndDate,
                WorkerCount = t.Workers.Count(),
                SafetyStewardName = t.SafetyStewardName
            });
        }

        // GET: /Safety/Dashboard
        public ActionResult Dashboard()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var names = _qms.GetDepartmentNames();

            var total = _db.PermitTasks.Count();
            var awaiting = _db.PermitTasks.Count(t => t.SafetyStewardName == null);

            var model = new SafetyDashboardDto
            {
                TotalTasks = total,
                AwaitingSteward = awaiting,
                StewardAssigned = total - awaiting,
                WorkersOnPermits = _db.PermitWorkers.Count(),

                LatestAwaiting = ToRows(_db.PermitTasks
                        .Where(t => t.SafetyStewardName == null)
                        .OrderByDescending(t => t.CreatedOn).ThenByDescending(t => t.Id)
                        .Take(10))
                    .ToList(),

                ByDepartment = _db.PermitTasks
                    .GroupBy(t => t.Permit.DepartmentId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList()
                    .Select(x => new DepartmentCountDto { Name = QmsDbContext.NameOf(names, x.Id), Count = x.Count })
                    .ToList()
            };

            foreach (var r in model.LatestAwaiting)
                r.DepartmentName = QmsDbContext.NameOf(names, r.DepartmentId);

            // work saved by departments per day, last 30 days (empty days = 0)
            var from = today.AddDays(-29);
            var perDay = _db.PermitTasks
                .Where(t => t.CreatedOn >= from && t.CreatedOn < tomorrow)
                .GroupBy(t => DbFunctions.TruncateTime(t.CreatedOn))
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList();

            model.LastDays = Enumerable.Range(0, 30)
                .Select(i => from.AddDays(i))
                .Select(d => new DayCountDto { Date = d, Count = perDay.Where(x => x.Date == d).Sum(x => x.Count) })
                .ToList();

            return View(model);
        }

        // GET: /Safety/Tasks?q=...&status=pending|done
        public ActionResult Tasks(string q, string status)
        {
            var query = _db.PermitTasks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(t => t.Permit.PermitNo.Contains(q) || t.Permit.WorkLocation.Contains(q)
                                      || t.SupervisorName.Contains(q) || t.SafetyStewardName.Contains(q));
            }

            if (status == "pending") query = query.Where(t => t.SafetyStewardName == null);
            else if (status == "done") query = query.Where(t => t.SafetyStewardName != null);

            var items = ToRows(query.OrderByDescending(t => t.CreatedOn).ThenByDescending(t => t.Id).Take(200)).ToList();

            var names = _qms.GetDepartmentNames();
            foreach (var r in items) r.DepartmentName = QmsDbContext.NameOf(names, r.DepartmentId);

            return View(new SafetyTaskListDto { Items = items, Query = q, Status = status });
        }

        // POST: /Safety/AssignSteward  - one row: pick a job, type the steward, Save
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AssignSteward(int taskId, string steward, string returnTo, string q, string status)
        {
            var task = _db.PermitTasks.Include(t => t.Permit).FirstOrDefault(t => t.Id == taskId);
            if (task == null) return HttpNotFound();

            steward = (steward ?? "").Trim();
            if (steward.Length == 0)
                TempData["Error"] = "Enter the Safety Steward name for permit " + task.Permit.PermitNo + ".";
            else if (steward.Length > 150)
                TempData["Error"] = "Safety Steward can be up to 150 characters.";
            else
            {
                task.SafetyStewardName = steward;
                task.SafetyUpdatedBy = User.Identity.Name;
                task.SafetyUpdatedOn = DateTime.Now;
                _db.SaveChanges();
                TempData["Flash"] = "Safety steward saved for permit " + task.Permit.PermitNo + ".";
            }

            if (returnTo == "tasks")
                return RedirectToAction("Tasks", new { q = q, status = status });
            return RedirectToAction("Dashboard");
        }

        // GET: /Safety/Steward/5   (5 = task id)
        public ActionResult Steward(int id)
        {
            var task = _db.PermitTasks.Include(t => t.Permit).Include(t => t.Workers).FirstOrDefault(t => t.Id == id);
            if (task == null) return HttpNotFound();

            var dto = new SafetyStewardDto { TaskId = task.Id, SafetyStewardName = task.SafetyStewardName };
            FillInfo(dto, task);
            return View(dto);
        }

        // POST: /Safety/Steward
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Steward(SafetyStewardDto model)
        {
            var task = _db.PermitTasks.Include(t => t.Permit).Include(t => t.Workers).FirstOrDefault(t => t.Id == model.TaskId);
            if (task == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.SafetyStewardName))
                ModelState.AddModelError("SafetyStewardName", "Safety steward is required");

            if (!ModelState.IsValid)
            {
                FillInfo(model, task);
                return View(model);
            }

            task.SafetyStewardName = model.SafetyStewardName.Trim();
            task.SafetyUpdatedBy = User.Identity.Name;
            task.SafetyUpdatedOn = DateTime.Now;
            _db.SaveChanges();

            TempData["Flash"] = "Safety steward saved for permit " + task.Permit.PermitNo + ".";
            return RedirectToAction("Tasks");
        }

        // GET: /Safety/Download/5   (5 = permit id)
        public ActionResult Download(int id)
        {
            var p = _db.Permits.FirstOrDefault(x => x.Id == id);
            if (p == null || p.AttachmentStoredName == null) return HttpNotFound();

            var path = Server.MapPath(AttachmentFolder + Path.GetFileName(p.AttachmentStoredName));
            if (!System.IO.File.Exists(path)) return HttpNotFound();

            var name = p.AttachmentFileName ?? p.AttachmentStoredName;
            return File(path, MimeMapping.GetMimeMapping(name), name);
        }

        private void FillInfo(SafetyStewardDto dto, PermitTask t)
        {
            var p = t.Permit;
            dto.IsEdit = t.SafetyStewardName != null;
            dto.PermitId = p.Id;
            dto.PermitNo = p.PermitNo;
            dto.DepartmentName = QmsDbContext.NameOf(_qms.GetDepartmentNames(), p.DepartmentId);
            dto.WorkLocation = p.WorkLocation;
            dto.Description = p.Description;
            dto.PermitIssueBy = p.PermitIssueBy;
            dto.PermitIssueTo = p.PermitIssueTo;
            dto.IssueDate = p.IssueDate;
            dto.AttachmentFileName = p.AttachmentStoredName == null ? null : p.AttachmentFileName;
            dto.SupervisorName = t.SupervisorName;
            dto.WorkStartDate = t.WorkStartDate;
            dto.ExpectedEndDate = t.ExpectedEndDate;
            dto.WorkerPassNos = t.Workers.OrderBy(w => w.Id).Select(w => w.PassNo).ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); _qms.Dispose(); }
            base.Dispose(disposing);
        }
    }
}