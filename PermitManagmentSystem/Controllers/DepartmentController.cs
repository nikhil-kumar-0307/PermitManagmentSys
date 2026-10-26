using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    [Authorize(Roles = AppRoles.Department)]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly QmsDbContext _qms = new QmsDbContext();

        private int _deptId;   // the logged-in user's department (Departments.Id in QMS)

        // Same folder the Operation side saves attachments in - change if yours is different
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

        // Reads the DepartmentId that AccountController.SignIn stored in the auth ticket (4th part of userData)
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

        // ---------- shared query: permit rows with their task info ----------
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
                WorkerCount = _db.PermitWorkers.Count(w => w.PermitTask.PermitId == p.Id)
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
                EndingThisWeek = myTasks.Count(t => t.ExpectedEndDate >= today && t.ExpectedEndDate <= weekEnd),
                WorkersDeployed = _db.PermitWorkers.Count(w => w.PermitTask.Permit.DepartmentId == _deptId),

                LatestAwaiting = ToRows(
                        mine.Where(p => !_db.PermitTasks.Any(t => t.PermitId == p.Id))
                            .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                            .Take(6))
                    .ToList(),

                Upcoming = myTasks
                    .Where(t => t.ExpectedEndDate >= today)
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

        // GET: /Department/Permits?q=...&status=pending|assigned
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

            if (status == "pending")
                query = query.Where(p => !_db.PermitTasks.Any(t => t.PermitId == p.Id));
            else if (status == "assigned")
                query = query.Where(p => _db.PermitTasks.Any(t => t.PermitId == p.Id));

            var model = new DepartmentPermitListDto
            {
                Query = q,
                Status = status,
                Items = ToRows(query.OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id).Take(200)).ToList()
            };
            return View(model);
        }

        // GET: /Department/Assign/5   (5 = permit id) - opens the task, or edits it if it already exists
        public ActionResult Assign(int id)
        {
            var permit = _db.Permits.FirstOrDefault(p => p.Id == id && p.DepartmentId == _deptId);
            if (permit == null) return HttpNotFound();

            var dto = new AssignTaskDto { PermitId = permit.Id };
            FillPermitInfo(dto, permit);

            var task = _db.PermitTasks.Include(t => t.Workers).FirstOrDefault(t => t.PermitId == id);
            if (task != null)
            {
                dto.IsEdit = true;
                dto.WorkStartDate = task.WorkStartDate;
                dto.ExpectedEndDate = task.ExpectedEndDate;
                dto.SupervisorName = task.SupervisorName;
                dto.WorkerPassNos = task.Workers.OrderBy(w => w.Id).Select(w => w.PassNo).ToList();
            }
            else
            {
                dto.WorkStartDate = DateTime.Today;
            }
            if (dto.WorkerPassNos.Count == 0) dto.WorkerPassNos.Add("");

            return View(dto);
        }

        // POST: /Department/Assign
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Assign(AssignTaskDto model)
        {
            var permit = _db.Permits.FirstOrDefault(p => p.Id == model.PermitId && p.DepartmentId == _deptId);
            if (permit == null) return HttpNotFound();

            // clean the pass numbers: trim, drop blanks
            var passes = (model.WorkerPassNos ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            if (passes.Count == 0)
                ModelState.AddModelError("WorkerPassNos", "Add at least one worker pass no.");
            else if (passes.Any(s => s.Length > 50))
                ModelState.AddModelError("WorkerPassNos", "A pass no can be up to 50 characters.");
            else if (passes.Select(s => s.ToUpperInvariant()).Distinct().Count() != passes.Count)
                ModelState.AddModelError("WorkerPassNos", "The same pass no is entered more than once.");

            if (model.WorkStartDate.HasValue && model.ExpectedEndDate.HasValue
                && model.ExpectedEndDate.Value.Date < model.WorkStartDate.Value.Date)
                ModelState.AddModelError("ExpectedEndDate", "Expected end date cannot be before the start date.");

            var task = _db.PermitTasks.Include(t => t.Workers).FirstOrDefault(t => t.PermitId == permit.Id);

            if (!ModelState.IsValid)
            {
                FillPermitInfo(model, permit);
                model.IsEdit = task != null;
                if (model.WorkerPassNos == null || model.WorkerPassNos.Count == 0) model.WorkerPassNos = new List<string> { "" };
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
                _db.PermitWorkers.RemoveRange(task.Workers.ToList());   // replace the worker list
            }

            task.SupervisorName = model.SupervisorName.Trim();
            task.WorkStartDate = model.WorkStartDate.Value.Date;
            task.ExpectedEndDate = model.ExpectedEndDate.Value.Date;

            foreach (var pass in passes)
                task.Workers.Add(new PermitWorker { PassNo = pass });

            _db.SaveChanges();

            TempData["Flash"] = "Work saved for permit " + permit.PermitNo + ".";
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