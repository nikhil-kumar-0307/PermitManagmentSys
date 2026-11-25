using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Filters;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    [RoleAuthorize(Roles = AppRoles.Department)]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly QmsDbContext _qms = new QmsDbContext();

        private int _deptId;   // the logged-in user's department (Departments.Id in QMS)

        
        private const string AttachmentFolder = "~/App_Data/Permits/";

        // ---------- who is this user? ----------
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            int id;
            if (!TryGetDepartmentId(out id))
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Your account is not linked to a department.");
                return;
            }
            _deptId = id;
            ViewBag.DepartmentName = QmsDbContext.NameOf(_qms.GetDepartmentNames(), _deptId);
        }
        private bool TryGetDepartmentId(out int id)
        {
            id = 0;
            var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return false;

            FormsAuthenticationTicket ticket;
            try { ticket = FormsAuthentication.Decrypt(cookie.Value); }
            catch { return false; }
            if (ticket == null || ticket.Expired) return false;

            var parts = (ticket.UserData ?? "").Split('|');
            return parts.Length >= 4 && int.TryParse(parts[3], out id);
        }

        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }
        private IQueryable<DepartmentPermitRowDto> ToRows(IQueryable<Permit> permits)
        {
            return permits.Select(p => new DepartmentPermitRowDto
            {
                Id = p.Id,
                PermitNo = p.PermitNo,
                WorkLocation = p.WorkLocation,
                PermitIssueBy = p.PermitIssueBy,
                PermitIssueTo = p.PermitIssueTo,
                IssueDate = p.IssueDate,
                HasAttachment = p.AttachmentStoredName != null,
                HasTask = _db.PermitTasks.Any(t => t.PermitId == p.Id),
                SupervisorName = _db.PermitTasks.Where(t => t.PermitId == p.Id).Select(t => t.SupervisorName).FirstOrDefault(),
                ExpectedEndDate = _db.PermitTasks.Where(t => t.PermitId == p.Id).Select(t => (DateTime?)t.ExpectedEndDate).FirstOrDefault(),
                WorkerCount = _db.PermitWorkers.Count(w => w.PermitTask.PermitId == p.Id),
                SafetyStewardName = _db.PermitTasks.Where(t => t.PermitId == p.Id).Select(t => t.SafetyStewardName).FirstOrDefault(),
                SafetyStewardMobile = _db.PermitTasks.Where(t => t.PermitId == p.Id).Select(t => t.SafetyStewardMobile).FirstOrDefault(),
                ClosedOn = _db.PermitTasks.Where(t => t.PermitId == p.Id).Select(t => t.ClosedOn).FirstOrDefault(),
                FinalClosedOn = _db.PermitTasks.Where(t => t.PermitId == p.Id).Select(t => t.FinalClosedOn).FirstOrDefault()
            });
        }

        // GET: /Department/Dashboard
        public ActionResult Dashboard()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var weekEnd = today.AddDays(7);

            var mine = _db.Permits.Where(p => p.DepartmentId == _deptId);
            var myTasks = _db.PermitTasks.Where(t => t.Permit.DepartmentId == _deptId);

            var total = mine.Count();
            var assigned = myTasks.Count();

            var model = new DepartmentDashboardDto
            {
                DepartmentName = (string)ViewBag.DepartmentName,
                TotalPermits = total,
                Assigned = assigned,
                AwaitingAssignment = total - assigned,
                EndingThisWeek = myTasks.Count(t => t.ClosedOn == null && t.ExpectedEndDate >= today && t.ExpectedEndDate <= weekEnd),
                AwaitingSafety = myTasks.Count(t => t.SafetyStewardName == null),
                ReadyToClose = myTasks.Count(t => t.SafetyStewardName != null && t.ClosedOn == null),
                Closed = myTasks.Count(t => t.FinalClosedOn != null),

                ReadyList = ToRows(
                        mine.Where(p => _db.PermitTasks.Any(t => t.PermitId == p.Id && t.SafetyStewardName != null && t.ClosedOn == null))
                            .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                            .Take(10))
                    .ToList(),
                WorkersDeployed = _db.PermitWorkers.Count(w => w.PermitTask.Permit.DepartmentId == _deptId),

                LatestAwaiting = ToRows(
                        mine.Where(p => !_db.PermitTasks.Any(t => t.PermitId == p.Id))
                            .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                            .Take(6))
                    .ToList(),

                Upcoming = myTasks
                    .Where(t => t.ClosedOn == null && t.ExpectedEndDate >= today)
                    .OrderBy(t => t.ExpectedEndDate).ThenBy(t => t.Id)
                    .Take(5)
                    .Select(t => new UpcomingWorkDto
                    {
                        PermitId = t.PermitId,
                        PermitNo = t.Permit.PermitNo,
                        WorkLocation = t.Permit.WorkLocation,
                        SupervisorName = t.SupervisorName,
                        ExpectedEndDate = t.ExpectedEndDate,
                        WorkerCount = t.Workers.Count()
                    }).ToList()
            };

            // permits received per day, last 30 days (empty days = 0)
            var from = today.AddDays(-29);
            var perDay = mine
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

        // GET: /Department/Permits?q=...&status=pending|safety|ready|closing|closed
        public ActionResult Permits(string q, string status)
        {
            var query = _db.Permits.Where(p => p.DepartmentId == _deptId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.PermitNo.Contains(q) || p.WorkLocation.Contains(q)
                                      || p.Description.Contains(q) || p.PermitIssueTo.Contains(q)
                                      || p.PermitIssueBy.Contains(q));
            }

            if (status == "pending")        // department has not assigned the work yet
                query = query.Where(p => !_db.PermitTasks.Any(t => t.PermitId == p.Id));
            else if (status == "safety")    // waiting for the Safety steward
                query = query.Where(p => _db.PermitTasks.Any(t => t.PermitId == p.Id && t.SafetyStewardName == null));
            else if (status == "ready")     // steward assigned, can be closed
                query = query.Where(p => _db.PermitTasks.Any(t => t.PermitId == p.Id && t.SafetyStewardName != null && t.ClosedOn == null));
            else if (status == "closing")   // closed by department, waiting for Safety final close
                query = query.Where(p => _db.PermitTasks.Any(t => t.PermitId == p.Id && t.ClosedOn != null && t.FinalClosedOn == null));
            else if (status == "closed")    // fully closed by Safety
                query = query.Where(p => _db.PermitTasks.Any(t => t.PermitId == p.Id && t.FinalClosedOn != null));

            var model = new DepartmentPermitListDto
            {
                Query = q,
                Status = status,
                Items = ToRows(query.OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id).Take(200)).ToList()
            };
            return View(model);
        }

        // GET: /Department/Assign/5   (5 = permit id)
        // new task -> form, existing task -> edit form, steward assigned -> read-only view
        public ActionResult Assign(int id)
        {
            var permit = _db.Permits.FirstOrDefault(p => p.Id == id && p.DepartmentId == _deptId);
            if (permit == null) return HttpNotFound();

            var dto = new AssignTaskDto { PermitId = permit.Id };
            FillPermitInfo(dto, permit);

            var task = _db.PermitTasks.Include(t => t.Workers).FirstOrDefault(t => t.PermitId == id);
            if (task != null && task.ClosedOn != null)
            {
                TempData["Error"] = "Permit " + permit.PermitNo + " is closed and can no longer be edited.";
                return RedirectToAction("Permits");
            }
            if (task != null)
            {
                dto.IsEdit = true;
                dto.WorkStartDate = task.WorkStartDate;
                dto.ExpectedEndDate = task.ExpectedEndDate;
                dto.SupervisorName = task.SupervisorName;
                dto.Workers = task.Workers
                    .OrderBy(w => w.Id)
                    .Select(w => new WorkerInputDto { PassNo = w.PassNo, WorkerName = w.WorkerName })
                    .ToList();

                // Safety has assigned the steward: show the steward and lock the work
                dto.SafetyStewardName = task.SafetyStewardName;
                dto.SafetyStewardMobile = task.SafetyStewardMobile;
                dto.IsLocked = !string.IsNullOrEmpty(task.SafetyStewardName);
            }
            else
            {
                dto.WorkStartDate = DateTime.Today;
            }
            if (dto.Workers.Count == 0) dto.Workers.Add(new WorkerInputDto());

            return View(dto);
        }

        // POST: /Department/Assign
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Assign(AssignTaskDto model)
        {
            var permit = _db.Permits.FirstOrDefault(p => p.Id == model.PermitId && p.DepartmentId == _deptId);
            if (permit == null) return HttpNotFound();

            var task = _db.PermitTasks.Include(t => t.Workers).FirstOrDefault(t => t.PermitId == permit.Id);

            if (task != null && task.ClosedOn != null)
            {
                TempData["Error"] = "Permit " + permit.PermitNo + " is closed and can no longer be edited.";
                return RedirectToAction("Permits");
            }

            // once Safety has assigned the steward the work can no longer be edited
            if (task != null && !string.IsNullOrEmpty(task.SafetyStewardName))
            {
                TempData["Error"] = "A Safety Steward is assigned to permit " + permit.PermitNo + ". The work can no longer be edited.";
                return RedirectToAction("Permits");
            }

            // clean the rows: trim, drop rows where both boxes are empty
            var workers = (model.Workers ?? new List<WorkerInputDto>())
                .Where(w => w != null && (!string.IsNullOrWhiteSpace(w.PassNo) || !string.IsNullOrWhiteSpace(w.WorkerName)))
                .Select(w => new WorkerInputDto
                {
                    PassNo = (w.PassNo ?? "").Trim(),
                    WorkerName = (w.WorkerName ?? "").Trim()
                })
                .ToList();

            if (workers.Count == 0)
                ModelState.AddModelError("Workers", "Add at least one worker.");
            else if (workers.Any(w => w.PassNo.Length == 0))
                ModelState.AddModelError("Workers", "Every worker needs a pass no.");
            else if (workers.Any(w => w.WorkerName.Length == 0))
                ModelState.AddModelError("Workers", "Every worker needs a name.");
            else if (workers.Any(w => w.PassNo.Length > 50))
                ModelState.AddModelError("Workers", "A pass no can be up to 50 characters.");
            else if (workers.Any(w => w.WorkerName.Length > 150))
                ModelState.AddModelError("Workers", "A worker name can be up to 150 characters.");
            else if (workers.Select(w => w.PassNo.ToUpperInvariant()).Distinct().Count() != workers.Count)
                ModelState.AddModelError("Workers", "The same pass no is entered more than once.");

            if (model.WorkStartDate.HasValue && model.ExpectedEndDate.HasValue
                && model.ExpectedEndDate.Value.Date < model.WorkStartDate.Value.Date)
                ModelState.AddModelError("ExpectedEndDate", "Expected end date cannot be before the start date.");

            if (!ModelState.IsValid)
            {
                FillPermitInfo(model, permit);
                model.IsEdit = task != null;
                model.IsLocked = false;                
                model.Workers = workers.Count == 0 ? new List<WorkerInputDto> { new WorkerInputDto() } : workers;
                // the view reads values from the model, so clear stale posted values
                ModelState.Remove("Workers");
                return View(model);
            }

            if (task == null)
            {
                task = new PermitTask { PermitId = permit.Id, AssignedBy = User.Identity.Name, CreatedOn = DateTime.Now };
                _db.PermitTasks.Add(task);
            }
            else
            {
                task.UpdatedOn = DateTime.Now;
                _db.PermitWorkers.RemoveRange(task.Workers.ToList());  
            }

            task.SupervisorName = model.SupervisorName.Trim();
            task.WorkStartDate = model.WorkStartDate.Value.Date;
            task.ExpectedEndDate = model.ExpectedEndDate.Value.Date;

            foreach (var w in workers)
                task.Workers.Add(new PermitWorker { PassNo = w.PassNo, WorkerName = w.WorkerName });

            _db.SaveChanges();

            TempData["Flash"] = "Work saved for permit " + permit.PermitNo + ".";
            return RedirectToAction("Permits");
        }

        // POST: /Department/Close  (id = permit id)
        // Only after Safety has assigned the steward. After this, Safety does the final close.
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Close(int id, string returnTo)
        {
            var permit = _db.Permits.FirstOrDefault(p => p.Id == id && p.DepartmentId == _deptId);
            if (permit == null) return HttpNotFound();

            var task = _db.PermitTasks.FirstOrDefault(t => t.PermitId == id);

            if (task == null)
                TempData["Error"] = "Assign the work for permit " + permit.PermitNo + " before closing it.";
            else if (task.ClosedOn != null)
                TempData["Error"] = "Permit " + permit.PermitNo + " is already closed.";
            else if (string.IsNullOrEmpty(task.SafetyStewardName))
                TempData["Error"] = "Safety has not assigned a steward to permit " + permit.PermitNo + " yet.";
            else
            {
                task.ClosedOn = DateTime.Now;
                task.ClosedBy = User.Identity.Name;
                _db.SaveChanges();
                TempData["Flash"] = "Permit " + permit.PermitNo + " is closed and sent to Operation for the final close.";
            }

            if (returnTo == "dashboard") return RedirectToAction("Dashboard");
            return RedirectToAction("Permits");
        }

        // GET: /Department/Download/5 - the permit document (own department only)
        public ActionResult Download(int id)
        {
            var p = _db.Permits.FirstOrDefault(x => x.Id == id && x.DepartmentId == _deptId);
            if (p == null || p.AttachmentStoredName == null) return HttpNotFound();

            var path = Server.MapPath(AttachmentFolder + Path.GetFileName(p.AttachmentStoredName));
            if (!System.IO.File.Exists(path)) return HttpNotFound();

            var name = p.AttachmentFileName ?? p.AttachmentStoredName;
            return File(path, MimeMapping.GetMimeMapping(name), name);
        }

        private void FillPermitInfo(AssignTaskDto dto, Permit p)
        {
            dto.PermitId = p.Id;
            dto.PermitNo = p.PermitNo;
            dto.WorkLocation = p.WorkLocation;
            dto.Description = p.Description;
            dto.PermitIssueBy = p.PermitIssueBy;
            dto.PermitIssueTo = p.PermitIssueTo;
            dto.IssueDate = p.IssueDate;
            dto.AttachmentFileName = p.AttachmentStoredName == null ? null : p.AttachmentFileName;
            dto.DepartmentName = QmsDbContext.NameOf(_qms.GetDepartmentNames(), p.DepartmentId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); _qms.Dispose(); }
            base.Dispose(disposing);
        }
    }
}