using EmployeeAdminPortal.Data;
using EmployeeAdminPortal.Models;
using EmployeeAdminPortal.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAdminPortal.Controllers
{
    /// <summary>
    /// Employee management API endpoints
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class EmployeeController : ControllerBase
    {
        public EmployeeController(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public ApplicationDbContext DbContext { get; }

        /// <summary>
        /// Get all employees
        /// </summary>
        /// <returns>List of all employees</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await DbContext.Employees.ToListAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Retrieved {employees.Count} employees successfully",
                    data = employees,
                    count = employees.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving employees",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get employee with the highest salary
        /// </summary>
        /// <returns>Employee with highest salary</returns>
        [HttpGet("highest-salary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeWithHighestSalary()
        {
            try
            {
                var employeeWithMinSalary = await DbContext.Employees
                    .OrderBy(e => e.Salary)
                    .FirstOrDefaultAsync();

                if (employeeWithMinSalary == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No employees found in the database"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Employee with lowest salary retrieved successfully",
                    data = employeeWithMinSalary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving the employee with highest salary",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get employee by name
        /// </summary>
        /// <param name="request">Employee name request</param>
        /// <returns>Employee with matching name</returns>
        [HttpPost("by-name")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeByName([FromBody] GetEmployeeByNameDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Employee name is required and cannot be empty"
                    });
                }

                var employee = await DbContext.Employees
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == request.Name.ToLower());

                if (employee == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Employee with name '{request.Name}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Employee '{request.Name}' found successfully",
                    data = employee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving the employee",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new employee
        /// </summary>
        /// <param name="addEmployeeDto">Employee details</param>
        /// <returns>Created employee</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        {
            try
            {
                // Check if model state is valid
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage);

                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = errors
                    });
                }

                // Validate input
                if (addEmployeeDto == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Employee data is required"
                    });
                }

                // Check if employee name already exists (case-insensitive)
                var existingEmployee = await DbContext.Employees
                    .FirstOrDefaultAsync(e => e.Name.ToLower() == addEmployeeDto.Name.ToLower());

                if (existingEmployee != null)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"Employee with name '{addEmployeeDto.Name}' already exists"
                    });
                }

                // Check if employee email already exists (case-insensitive)
                var existingEmailEmployee = await DbContext.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == addEmployeeDto.Email.ToLower());

                if (existingEmailEmployee != null)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"Employee with email '{addEmployeeDto.Email}' already exists"
                    });
                }

                var employeeEntity = new Employee()
                {
                    Id = Guid.NewGuid(),
                    Name = addEmployeeDto.Name.Trim(),
                    Email = addEmployeeDto.Email.Trim().ToLower(),
                    Phone = addEmployeeDto.Phone?.Trim(),
                    Salary = addEmployeeDto.Salary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                DbContext.Employees.Add(employeeEntity);
                await DbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEmployees), new { id = employeeEntity.Id }, new
                {
                    success = true,
                    message = $"Employee '{addEmployeeDto.Name}' created successfully",
                    data = employeeEntity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the employee",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete an employee by ID
        /// </summary>
        /// <param name="id">Employee ID</param>
        /// <returns>Success message</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteEmployee([FromQuery][Required] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Valid employee ID is required"
                    });
                }

                var employee = await DbContext.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Employee with ID '{id}' not found"
                    });
                }

                DbContext.Employees.Remove(employee);
                await DbContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Employee '{employee.Name}' deleted successfully",
                    deletedEmployee = employee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the employee",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Health check endpoint for the API
        /// </summary>
        /// <returns>API status</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Test database connection
                var canConnect = await DbContext.Database.CanConnectAsync();
                var employeeCount = canConnect ? await DbContext.Employees.CountAsync() : 0;

                return Ok(new
                {
                    success = true,
                    message = "Employee API is running successfully! ??",
                    timestamp = DateTime.UtcNow,
                    database = new
                    {
                        connected = canConnect,
                        employeeCount = employeeCount,
                        provider = DbContext.Database.ProviderName
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Health check failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}