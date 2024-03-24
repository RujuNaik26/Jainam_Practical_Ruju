using EmployeeManager.Data;
using EmployeeManager.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EmployeeManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        EmployeeContext _employeeContext = new EmployeeContext();
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> Index(EmpoyeeModel _empoyeeModel)
        {
            var status = await _employeeContext.EmployeeLogin.Where(m => m.LoginId == _empoyeeModel.LoginId && m.Password == _empoyeeModel.Pasword).FirstOrDefaultAsync();
            if (status == null)
            {
                ViewBag.LoginStatus = 0;
            }
            else
            {
                HttpContext.Session.SetString("UserName",status.EmployeeName);
                HttpContext.Session.SetString("LoginId", status.LoginId);
                ViewBag.UserName = HttpContext.Session.GetString("UserName");
                return View("EmployeeView", "Home");
            }
            return View(_empoyeeModel);
        }

        /// <summary>
        /// Get Employee Details
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<EmployeeResult>> GetEmployeeDetails()
        {
            List<EmployeeResult> employeeResult = new List<EmployeeResult>();
            try
            {
                List<EmployeeDetailModel> employeeDetailList = await _employeeContext.EmployeeDetail.ToListAsync();
                if (employeeDetailList.Any())
                {
                    employeeResult.Add(new EmployeeResult
                    {
                        empoyeeModel = employeeDetailList,
                        IsSuccess = true
                    });
                }
                else
                {
                    employeeResult.Add(new EmployeeResult
                    {
                        IsSuccess = false
                    });
                }
                return employeeResult;
            }
            catch (Exception)
            {
                employeeResult.Add(new EmployeeResult { IsSuccess = false , Message = "Something went wrong!!" });
                return employeeResult;
            }
        }

        /// <summary>
        /// Import CSV File
        /// </summary>
        /// <param name="postedFile"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<bool> ImportData(IFormFile postedFile)
        {
            List<EmployeeDetailModel> employeeDetailList = new List<EmployeeDetailModel>();
            string recordInserted = null;
            bool isSuccess = false;
            try
            {
                if (postedFile != null)
                {
                    string path = Path.Combine(Path.GetFullPath("wwwroot"), "Uploads");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string fileName = Path.GetFileName(postedFile.FileName);
                    string filePath = Path.Combine(path, fileName);
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        postedFile.CopyTo(stream);
                    }
                    string csvData = System.IO.File.ReadAllText(filePath);
                    bool isFirstRow = true;

                    foreach (string row in csvData.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(row))
                        {
                            if (isFirstRow)
                            {
                                isFirstRow = false;
                                continue;
                            }
                            string[] fields = row.Split(',');
                            if (fields.Length >= 1)
                            {
                                employeeDetailList.Add(new EmployeeDetailModel
                                {
                                    EmployeeId = !string.IsNullOrEmpty(fields[0]) ? fields[0] : null,
                                    EmployeeName = !string.IsNullOrEmpty(fields[1]) ? fields[1] : null,
                                    Department = !string.IsNullOrEmpty(fields[2]) ? fields[2] : null,
                                    EmployeeEmail = !string.IsNullOrEmpty(fields[3]) ? fields[3] : null,
                                    JobTitle = !string.IsNullOrEmpty(fields[4]) ? fields[4] : null,
                                    PhoneNumber = !string.IsNullOrEmpty(fields[5]) ? fields[5] : null,
                                });
                            }
                        }
                    }
                    recordInserted = await InsertEmployee(employeeDetailList);
                    if (!string.IsNullOrEmpty(recordInserted) && recordInserted=="success")
                    {
                        isSuccess = true;
                    }
                }
            }
            catch (Exception)
            {
                isSuccess = false;
                return isSuccess;
            }
            return isSuccess;
        }
        
        /// <summary>
        /// Insert Employee
        /// </summary>
        /// <param name="employeeDetailList"></param>
        /// <returns></returns>
        public async Task<string> InsertEmployee(List<EmployeeDetailModel> employeeDetailList)
        {
            try
            {
                using (var transaction = await _employeeContext.Database.BeginTransactionAsync())
                {
                    foreach (var employee in employeeDetailList)
                    {
                        await _employeeContext.Database.ExecuteSqlInterpolatedAsync($@"EXEC InsertOrUpdateEmployee 
                        {0},
                        {employee.EmployeeId}, 
                        {employee.EmployeeName}, 
                        {employee.Department},
                        {employee.EmployeeEmail},
                        {employee.JobTitle},
                        {employee.PhoneNumber}");
                    }
                    transaction.Commit();
                }
                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Update Employee
        /// </summary>
        /// <param name="employeeDetail"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeDetailModel employeeDetail)
        {
            string UserName = HttpContext.Session.GetString("UserName");
            //string LoginId = HttpContext.Session.GetString("LoginId");
            try
            {
                if (employeeDetail != null)
                {
                    employeeDetail.UpdatedBy = UserName;
                    var transaction = await _employeeContext.Database.BeginTransactionAsync();
                    await _employeeContext.Database.ExecuteSqlInterpolatedAsync($@"EXEC InsertOrUpdateEmployee
                    {employeeDetail.Id},
                    {employeeDetail.EmployeeId},
                    {employeeDetail.EmployeeName},
                    {employeeDetail.Department},
                    {employeeDetail.EmployeeEmail},
                    {employeeDetail.JobTitle},
                    {employeeDetail.PhoneNumber},
                    {employeeDetail.UpdatedBy}"); 
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                return Json("Something went wrong!!");
            }
            return Json("success");
        }
        
        /// <summary>
        /// Edit Employee
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditEmployee(int employeeId)
        {
            EmployeeResult employeeResult = new EmployeeResult();
            try
            {
                EmployeeDetailModel employee = new EmployeeDetailModel();
                employee = await _employeeContext.EmployeeDetail.Where(x => x.Id == employeeId).FirstOrDefaultAsync();
                if (employee != null)
                {
                    employeeResult.empoyeeModel = new List<EmployeeDetailModel>
                    {
                        employee
                    };
                    employeeResult.IsSuccess = true;
                    return Json(employeeResult);
                }
                else
                {
                    employeeResult.IsSuccess = false;
                    return Json(employeeResult);
                }
            }
            catch (Exception)
            {
                employeeResult.IsSuccess = false;
                return Json(employeeResult);
            }
        }
        
        /// <summary>
        /// Delete Employee
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteEmployee(int employeeId)
        {
            try
            {
                var data = await _employeeContext.EmployeeDetail.Where(x=>x.Id== employeeId).FirstOrDefaultAsync();
                if(data != null)
                {
                     _employeeContext.EmployeeDetail.Remove(data);
                     await _employeeContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
            return Json("success");
        }

        /// <summary>
        /// Employee List View
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> EmployeeView()
        {
            List<EmployeeDetailModel> employeeDetailList = await _employeeContext.EmployeeDetail.ToListAsync();
            string serializedData = JsonConvert.SerializeObject(employeeDetailList);
            TempData["employeeDetailList"] = serializedData;
            return View();
        }
    }
}
