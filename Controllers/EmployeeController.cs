using EmployeeAdminPortal.Data;
using EmployeeAdminPortal.Models;
using EmployeeAdminPortal.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeAdminPortal.Controllers
{
    //localhost:xxxx/api/employees
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        public EmployeeController(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ApplicationDbContext DbContext { get; }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await DbContext.Employees.ToListAsync();
            return Ok(employees);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee(AddEmployeeDto addEmployeeDto)
        {
            try
            {
                var employeeEntiry = new Employee()
                {
                    Id = Guid.NewGuid(),
                    Name = addEmployeeDto.Name,
                    Email = addEmployeeDto.Email,
                    Phone = addEmployeeDto.Phone,
                    Salary = addEmployeeDto.Salary
                };

                DbContext.Employees.Add(employeeEntiry);
                await DbContext.SaveChangesAsync();
                return CreatedAtAction(nameof(GetEmployees), new { id = employeeEntiry.Id }, employeeEntiry);
            }
            catch (Exception ex)
            {

                return BadRequest();
            }


        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEmployee(Guid id)
        {
            var employee = await DbContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound("Employee not found.");
            }
            DbContext.Employees.Remove(employee);
            await DbContext.SaveChangesAsync();
            return NoContent();
        }
        [HttpGet("highest-salary")]
        public async Task<IActionResult> GetEmployeeWithHighestSalary()
        {
            try
            {
                var employeeWithMaxSalary = await DbContext.Employees
                    .OrderByDescending(e => e.Salary)
                    .FirstOrDefaultAsync();

                if (employeeWithMaxSalary == null)
                {
                    return NotFound("No employees found.");
                }

                return Ok(employeeWithMaxSalary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the employee with highest salary.");
            }
        }

        [HttpPost("by-name")]
        public async Task<IActionResult> GetEmployeeByName([FromBody] GetEmployeeByNameDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("Employee name is required.");
                }

                var employee = await DbContext.Employees
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == request.Name.ToLower());

                if (employee == null)
                {
                    return NotFound($"Employee with name '{request.Name}' not found.");
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the employee.");
            }
        }

    }
}
