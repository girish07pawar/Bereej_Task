﻿namespace EmployeeAdminPortal.Models
{
    public class AddEmployeeDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Phone { get; set; }
        public decimal Salary { get; set; }
        // Additional properties can be added as needed
    }

    //public class GetEmployeeByNameDto
    //{
    //    public string Name { get; set; } = string.Empty;
    //}

}
