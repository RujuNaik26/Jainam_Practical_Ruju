using System.Collections.Generic;

namespace EmployeeManager.Models
{
    public class EmployeeDetailModel
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? Department { get; set; }
        public string? EmployeeEmail { get; set; }
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class EmployeeResult
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeDetailModel> empoyeeModel { get; set; }
        public string Message { get; set; } 

    }
}
