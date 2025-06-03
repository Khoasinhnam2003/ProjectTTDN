using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNV.Data;
using QLNV.Models;
using QLNV.Models.Entities;
using QLNV.Repositories;

namespace QLNV.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        //private readonly ApplicationDbContext dbContext;

        //public EmployeesController(ApplicationDbContext dbContext) {
        //    this.dbContext = dbContext;
        //}
        //[HttpGet]
        //public IActionResult GetAllEmployees()
        //{
        //    var allEmployees = dbContext.Employees.ToList();

        //    return Ok(allEmployees);
        //}
        //[HttpGet("{id:guid}")]
        //public IActionResult GetEmployeeById(Guid id)
        //{
        //    var employee = dbContext.Employees.Find(id);

        //    if (employee == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(employee);
        //}

        //[HttpPost]
        //public IActionResult AddEmployee(AddEmployeeDto addEmployeeDto )
        //{
        //    var employeeEntity = new Employee()
        //    {
        //        Name = addEmployeeDto.Name,
        //        Email = addEmployeeDto.Email,
        //        Phone = addEmployeeDto.Phone,
        //        Salary = addEmployeeDto.Salary
        //    };


        //    dbContext.Employees.Add(employeeEntity);
        //    dbContext.SaveChanges();

        //    return Ok(employeeEntity);
        //}
        //[HttpPut("{id:guid}")]
        //public IActionResult UpdateEmployee(Guid id, UpdateEmployeeDto updateEmployeeDto)
        //{
        //    var employee = dbContext.Employees.Find(id);

        //    if(employee == null)
        //    {
        //        return NotFound();
        //    }
        //    employee.Name = updateEmployeeDto.Name;
        //    employee.Email = updateEmployeeDto.Email;
        //    employee.Phone = updateEmployeeDto.Phone;
        //    employee.Salary = updateEmployeeDto.Salary;

        //    dbContext.SaveChanges();

        //    return Ok(employee);
        //}
        //[HttpDelete("{id:guid}")]
        //public IActionResult DeleteEmployee(Guid id)
        //{
        //    var employee = dbContext.Employees.Find(id);

        //    if(employee == null)
        //    {
        //        return NotFound();
        //    }
        //    dbContext.Employees.Remove(employee);
        //    dbContext.SaveChanges();
        //    return Ok();
        //}
        //------------------------------------------------------------------------

        //private readonly ApplicationDbContext _dbContext;

        //public EmployeesController(ApplicationDbContext dbContext)
        //{
        //    _dbContext = dbContext;
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetAllEmployees()
        //{
        //    var allEmployees = await _dbContext.Employees.ToListAsync();
        //    return Ok(allEmployees);
        //}

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> GetEmployeeById(int id)
        //{
        //    var employee = await _dbContext.Employees.FindAsync(id);
        //    if (employee == null) return NotFound();
        //    return Ok(employee);
        //}

        //[HttpPost]
        //public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var employeeEntity = new Employee
        //    {
        //        Name = addEmployeeDto.Name,
        //        Email = addEmployeeDto.Email,
        //        Phone = addEmployeeDto.Phone,
        //        Salary = addEmployeeDto.Salary
        //    };

        //    _dbContext.Employees.Add(employeeEntity);
        //    await _dbContext.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetEmployeeById), new { id = employeeEntity.Id }, employeeEntity);
        //}

        //[HttpPut("{id:int}")]
        //public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var employee = await _dbContext.Employees.FindAsync(id);
        //    if (employee == null) return NotFound();

        //    employee.Name = updateEmployeeDto.Name;
        //    employee.Email = updateEmployeeDto.Email;
        //    employee.Phone = updateEmployeeDto.Phone;
        //    employee.Salary = updateEmployeeDto.Salary;

        //    await _dbContext.SaveChangesAsync();
        //    return Ok(employee);
        //}

        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> DeleteEmployee(int id)
        //{
        //    var employee = await _dbContext.Employees.FindAsync(id);
        //    if (employee == null) return NotFound();

        //    _dbContext.Employees.Remove(employee);
        //    await _dbContext.SaveChangesAsync();
        //    return Ok();
        //}

        //[HttpGet("filter-by-salary")]
        //public async Task<IActionResult> GetEmployeesBySalary(decimal minSalary)
        //{
        //    var employees = await _dbContext.Employees
        //        .Where(e => e.Salary >= minSalary)
        //        .OrderBy(e => e.Salary)
        //        .ToListAsync();

        //    return Ok(employees);
        //}

        //[HttpGet("search-by-name")]
        //public async Task<IActionResult> SearchEmployeesByName(string name)
        //{
        //    if (string.IsNullOrEmpty(name)) return BadRequest("Name is required");

        //    var employees = await _dbContext.Employees
        //        .Where(e => e.Name.ToLower().Contains(name.ToLower()))
        //        .ToListAsync();

        //    return Ok(employees);
        //}


        //---------------------------------------------------------------------------------

        private readonly IEmployeeRepository _employeeRepository;

        public EmployeesController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employees = await _employeeRepository.GetAllAsync();
            return Ok(employees);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null) return NotFound();
            return Ok(employee);
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employeeEntity = new Employee
            {
                Name = addEmployeeDto.Name,
                Email = addEmployeeDto.Email,
                Phone = addEmployeeDto.Phone,
                Salary = addEmployeeDto.Salary
            };

            var createdEmployee = await _employeeRepository.AddAsync(employeeEntity);
            return CreatedAtAction(nameof(GetEmployeeById), new { id = createdEmployee.Id }, createdEmployee);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = new Employee
            {
                Id = id,
                Name = updateEmployeeDto.Name,
                Email = updateEmployeeDto.Email,
                Phone = updateEmployeeDto.Phone,
                Salary = updateEmployeeDto.Salary
            };

            var updatedEmployee = await _employeeRepository.UpdateAsync(id, employee);
            if (updatedEmployee == null) return NotFound();
            return Ok(updatedEmployee);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var deleted = await _employeeRepository.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok();
        }

        [HttpGet("filter-by-salary")]
        public async Task<IActionResult> GetEmployeesBySalary(decimal minSalary)
        {
            if (minSalary < 0) return BadRequest("Minimum salary cannot be negative");

            var employees = await _employeeRepository.GetBySalaryAsync(minSalary);
            return Ok(employees);
        }

        [HttpGet("search-by-name")]
        public async Task<IActionResult> SearchEmployeesByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return BadRequest("Name is required");

            var employees = await _employeeRepository.SearchByNameAsync(name);
            return Ok(employees);
        }
    }
}
