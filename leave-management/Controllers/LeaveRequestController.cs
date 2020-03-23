using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using leave_management.Contracts;
using leave_management.Data;
using leave_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace leave_management.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ILeaveRequestRepository _leaveRequestRepo;
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly ILeaveAllocationRepository _leaveAllocRepo;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestController(
            ILeaveRequestRepository leaveRequestRepo,
            ILeaveTypeRepository leaveTypeRepository,
            ILeaveAllocationRepository leaveAllocRepo,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            _leaveRequestRepo = leaveRequestRepo;
            _leaveTypeRepo = leaveTypeRepository;
            _leaveAllocRepo = leaveAllocRepo;
            _mapper = mapper;
            _userManager = userManager;
    }
        [Authorize(Roles = "Administrator")]
        // GET: LeaveRequest
        public ActionResult Index()
        {
            var leaverequests = _leaveRequestRepo.FindAll();
            var leaveRequestsModel = _mapper.Map<List<LeaveRequestVM>>(leaverequests);
            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequestsModel.Count,
                ApprovedRequests = leaveRequestsModel.Count(q => q.Approved == true), //cuento todos los regisrros que cumplen esa condicion
                PendingRequests = leaveRequestsModel.Count(q => q.Approved == null),
                RejectedRequests = leaveRequestsModel.Count(q => q.Approved == false),
                LeaveRequests = leaveRequestsModel
            };
            return View(model);
        }

        // GET: LeaveRequest/Details/5
        public ActionResult Details(int id)
        {
            var leaverequest = _leaveRequestRepo.FindById(id);
            var model = _mapper.Map<LeaveRequestVM>(leaverequest);
            return View(model);
        }

        public ActionResult ApproveRequest(int id)
        {
            try
            {
                var user = _userManager.GetUserAsync(User).Result;
                var leaverequest = _leaveRequestRepo.FindById(id);
                var employeeid = leaverequest.RequestingEmployeeId;
                var leavetypeid = leaverequest.LeaveTypeId;
                var allocation = _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leavetypeid);

                leaverequest.Approved = true;
                leaverequest.ApprovedById = user.Id;
                leaverequest.DateActioned = DateTime.Now;
                var isSuccess = _leaveRequestRepo.Update(leaverequest);
                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
            
        }

        public ActionResult RejectRequest(int id)
        {
            try
            {
                var user = _userManager.GetUserAsync(User).Result;
                var leaverequest = _leaveRequestRepo.FindById(id);
                var allocation = _leaveAllocRepo.GetLeaveAllocationsByEmployee(leaverequest.RequestingEmployeeId);

                leaverequest.Approved = false;
                leaverequest.ApprovedById = user.Id;
                leaverequest.DateActioned = DateTime.Now;
                var isSuccess = _leaveRequestRepo.Update(leaverequest);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: LeaveRequest/Create
        public ActionResult Create()
        {
            var leavetypes = _leaveTypeRepo.FindAll();
            var leaveTypeItems = leavetypes.Select(q => new SelectListItem
            {
                Text = q.Name,
                Value = q.Id.ToString()
            });
            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypeItems
            };
            return View(model);
        }

        // POST: LeaveRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateLeaveRequestVM model)
        {
            
            try
            {
                var startDate = Convert.ToDateTime(model.StartDate);
                var endDate = Convert.ToDateTime(model.EndDate);
                var leavetypes = _leaveTypeRepo.FindAll();
                var leaveTypeItems = leavetypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()
                });
                 model.LeaveTypes = leaveTypeItems;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if(DateTime.Compare(startDate, endDate) > 1)
                {
                    ModelState.AddModelError("", "Start Date cannot be further than the end date");
                    return View(model);
                }

                var employee = _userManager.GetUserAsync(User).Result; //buena manera de retornar el usuario actual
                var allocations = _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId); //retorno las allocations para este emplado
                int daysrequested = (int)(endDate - startDate).TotalDays;

                if(daysrequested > allocations.NumberOfDays)
                {
                    ModelState.AddModelError("", "You don't have sufficient days for this request");
                    return View(model);
                }

                var leaverequestModel = new LeaveRequestVM //esta es la parte en la que se guarda en la DB, esto es lo que se gaurda
                {
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    LeaveTypeId = model.LeaveTypeId
                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaverequestModel);
                var isSuccess = _leaveRequestRepo.Create(leaveRequest);

                if (!isSuccess)
                {
                    ModelState.AddModelError("", "Something went wrong with submitting your record");
                    return View();
                }

                return RedirectToAction(nameof(Index), "Home");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View();
            }
        }

        // GET: LeaveRequest/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveRequest/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveRequest/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}