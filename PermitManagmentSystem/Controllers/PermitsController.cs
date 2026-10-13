using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PermitManagmentSystem.Data;
using PermitManagmentSystem.Models;
using PermitManagmentSystem.Models.DTOs;

namespace PermitManagmentSystem.Controllers
{
    // All permit work is done by the Operation login only.
    // To let SuperAdmin in too:  [Authorize(Roles = AppRoles.Operation + "," + AppRoles.SuperAdmin)]
    [Authorize(Roles = AppRoles.Operation)]
    public class PermitsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly QmsDbContext _qms = new QmsDbContext();   // departments live in the QMS database

        private static readonly string[] AllowedExtensions =
            { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private const int MaxFileBytes = 5 * 1024 * 1024; // 5 MB

        // permits per page choices in the list (must match the drop-down in Index.cshtml)
        private static readonly int[] PageSizes = { 10, 15, 25, 50, 100 };
        private const int DefaultPageSize = 15;

        // ---------------------------------------------------------------- list
        // GET: /Permits?q=...&page=2&size=25
        public ActionResult Index(string q, int page = 1, int size = DefaultPageSize)
        {
            if (!PageSizes.Contains(size)) size = DefaultPageSize;

            var query = _db.Permits.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                // Department names are in the QMS database, so find the matching ids there first
                var deptIds = _qms.Departments
                    .Where(d => d.Name.Contains(q) || d.Code.Contains(q))
                    .Select(d => d.Id)
                    .ToList();

                query = query.Where(p =>
                    p.PermitNo.Contains(q) ||
                    p.WorkLocation.Contains(q) ||
                    deptIds.Contains(p.DepartmentId) ||
                    p.PermitIssueBy.Contains(q) ||
                    p.PermitIssueTo.Contains(q));
            }

            var total = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var list = query
                .OrderByDescending(p => p.IssueDate).ThenByDescending(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new PermitListDto
                {
                    Id = p.Id,
                    PermitNo = p.PermitNo,
                    WorkLocation = p.WorkLocation,
                    DepartmentId = p.DepartmentId,
                    Description = p.Description,
                    PermitIssueBy = p.PermitIssueBy,
                    PermitIssueTo = p.PermitIssueTo,
                    IssueDate = p.IssueDate,
                    HasAttachment = p.AttachmentStoredName != null,
                    AttachmentFileName = p.AttachmentFileName
                }).ToList();

            var names = _qms.GetDepartmentNames();
            foreach (var row in list)
                row.Department = QmsDbContext.NameOf(names, row.DepartmentId);

            return View(new PermitListPageDto
            {
                Items = list,
                Query = q,
                Page = page,
                PageSize = size,
                TotalCount = total
            });
        }

        // ---------------------------------------------------------------- add
        // GET: /Permits/Create
        public ActionResult Create()
        {
            return View(new PermitDto
            {
                IssueDate = DateTime.Today,
                Departments = DepartmentOptions(null)
            });
        }

        // POST: /Permits/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(PermitDto model)
        {
            var permitNo = (model.PermitNo ?? "").Trim();

            if (_db.Permits.Any(p => p.PermitNo == permitNo))
                ModelState.AddModelError("PermitNo", "This permit no already exists.");

            if (model.DepartmentId.HasValue)
            {
                var deptId = model.DepartmentId.Value;
                if (!_qms.Departments.Any(d => d.Id == deptId && d.IsActive))
                    ModelState.AddModelError("DepartmentId", "Select a department from the list.");
            }

            ValidateFile(model.Attachment);

            if (!ModelState.IsValid)
            {
                model.Departments = DepartmentOptions(model.DepartmentId);
                return View(model);
            }

            var permit = new Permit
            {
                PermitNo = permitNo,
                WorkLocation = model.WorkLocation.Trim(),
                DepartmentId = model.DepartmentId.Value,
                Description = model.Description.Trim(),
                PermitIssueBy = model.PermitIssueBy.Trim(),
                PermitIssueTo = model.PermitIssueTo.Trim(),
                IssueDate = model.IssueDate.Value.Date,
                CreatedBy = User.Identity.Name,
                CreatedOn = DateTime.Now
            };

            if (HasFile(model.Attachment))
            {
                permit.AttachmentFileName = Path.GetFileName(model.Attachment.FileName);
                permit.AttachmentStoredName = SaveFile(model.Attachment);
            }

            _db.Permits.Add(permit);
            _db.SaveChanges();

            TempData["Success"] = "Permit " + permit.PermitNo + " added.";
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------------- edit
        // GET: /Permits/Edit/5
        public ActionResult Edit(int id)
        {
            var permit = _db.Permits.Find(id);
            if (permit == null) return HttpNotFound();

            // 0 = an old permit created before departments were linked: force a choice
            int? selected = permit.DepartmentId == 0 ? (int?)null : permit.DepartmentId;

            return View(new PermitDto
            {
                Id = permit.Id,
                PermitNo = permit.PermitNo,
                WorkLocation = permit.WorkLocation,
                DepartmentId = selected,
                Departments = DepartmentOptions(selected),
                Description = permit.Description,
                PermitIssueBy = permit.PermitIssueBy,
                PermitIssueTo = permit.PermitIssueTo,
                IssueDate = permit.IssueDate,
                ExistingFileName = permit.AttachmentFileName
            });
        }

        // POST: /Permits/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, PermitDto model)
        {
            var permit = _db.Permits.Find(id);
            if (permit == null) return HttpNotFound();

            model.Id = id;
            var permitNo = (model.PermitNo ?? "").Trim();

            if (_db.Permits.Any(p => p.PermitNo == permitNo && p.Id != id))
                ModelState.AddModelError("PermitNo", "This permit no already exists.");

            if (model.DepartmentId.HasValue)
            {
                var deptId = model.DepartmentId.Value;
                var current = permit.DepartmentId;   // keep allowing the permit's current department even if now inactive
                if (!_qms.Departments.Any(d => d.Id == deptId && (d.IsActive || d.Id == current)))
                    ModelState.AddModelError("DepartmentId", "Select a department from the list.");
            }

            ValidateFile(model.Attachment);

            if (!ModelState.IsValid)
            {
                model.ExistingFileName = permit.AttachmentFileName;
                model.Departments = DepartmentOptions(model.DepartmentId ?? permit.DepartmentId);
                return View(model);
            }

            permit.PermitNo = permitNo;
            permit.WorkLocation = model.WorkLocation.Trim();
            permit.DepartmentId = model.DepartmentId.Value;
            permit.Description = model.Description.Trim();
            permit.PermitIssueBy = model.PermitIssueBy.Trim();
            permit.PermitIssueTo = model.PermitIssueTo.Trim();
            permit.IssueDate = model.IssueDate.Value.Date;
            permit.UpdatedOn = DateTime.Now;

            string oldStoredName = null;

            if (HasFile(model.Attachment))                 // replace the file
            {
                oldStoredName = permit.AttachmentStoredName;
                permit.AttachmentFileName = Path.GetFileName(model.Attachment.FileName);
                permit.AttachmentStoredName = SaveFile(model.Attachment);
            }
            else if (model.RemoveAttachment)               // just remove it
            {
                oldStoredName = permit.AttachmentStoredName;
                permit.AttachmentFileName = null;
                permit.AttachmentStoredName = null;
            }

            _db.SaveChanges();
            DeleteFile(oldStoredName);                     // only after the DB save succeeded

            TempData["Success"] = "Permit " + permit.PermitNo + " updated.";
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------------- delete
        // POST: /Permits/Delete   (confirmed from the dialog on the list page)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var permit = _db.Permits.Find(id);
            if (permit == null) return HttpNotFound();

            var stored = permit.AttachmentStoredName;
            var no = permit.PermitNo;

            _db.Permits.Remove(permit);
            _db.SaveChanges();
            DeleteFile(stored);

            TempData["Success"] = "Permit " + no + " deleted.";
            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------------- attachment
        // GET: /Permits/Download/5
        public ActionResult Download(int id)
        {
            var permit = _db.Permits.Find(id);
            if (permit == null || string.IsNullOrEmpty(permit.AttachmentStoredName))
                return HttpNotFound();

            var path = Path.Combine(UploadFolder(), permit.AttachmentStoredName);
            if (!System.IO.File.Exists(path)) return HttpNotFound();

            return File(path, MimeMapping.GetMimeMapping(permit.AttachmentFileName), permit.AttachmentFileName);
        }

        // ---------------------------------------------------------------- department drop-down
        // Active departments from QMS, plus the currently selected one (even if it was deactivated later)
        private List<SelectListItem> DepartmentOptions(int? selectedId)
        {
            var sel = selectedId ?? -1;

            return _qms.Departments
                .Where(d => d.IsActive || d.Id == sel)
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name, d.Code })
                .ToList()
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = string.IsNullOrEmpty(d.Code) ? d.Name : d.Name + " (" + d.Code + ")",
                    Selected = d.Id == sel
                })
                .ToList();
        }

        // ---------------------------------------------------------------- file helpers
        // Files live in App_Data, which IIS never serves directly,
        // so they can only be opened through Download() above (login required).
        private string UploadFolder()
        {
            var folder = Server.MapPath("~/App_Data/PermitAttachments");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static bool HasFile(HttpPostedFileBase file)
        {
            return file != null && file.ContentLength > 0;
        }

        private void ValidateFile(HttpPostedFileBase file)
        {
            if (!HasFile(file)) return;

            var ext = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
                ModelState.AddModelError("Attachment", "Upload a PDF, Word, Excel, JPG or PNG file.");
            else if (file.ContentLength > MaxFileBytes)
                ModelState.AddModelError("Attachment", "File is too large. The limit is 5 MB.");
        }

        private string SaveFile(HttpPostedFileBase file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = Guid.NewGuid().ToString("N") + ext;
            file.SaveAs(Path.Combine(UploadFolder(), storedName));
            return storedName;
        }

        private void DeleteFile(string storedName)
        {
            if (string.IsNullOrEmpty(storedName)) return;

            var path = Path.Combine(UploadFolder(), storedName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); _qms.Dispose(); }
            base.Dispose(disposing);
        }
    }
}