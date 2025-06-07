using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QLNV.Application.Commands;
using QLNV.Application.DTOs;
using QLNV.Application.Queries;
using QLNV.Application.Services;

namespace QLNV.Presentation.Controllers
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

        //private readonly IEmployeeRepository _employeeRepository;

        //public EmployeesController(IEmployeeRepository employeeRepository)
        //{
        //    _employeeRepository = employeeRepository;
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetAllEmployees()
        //{
        //    var employees = await _employeeRepository.GetAllAsync();
        //    return Ok(employees);
        //}

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> GetEmployeeById(int id)
        //{
        //    var employee = await _employeeRepository.GetByIdAsync(id);
        //    if (employee == null) return NotFound();
        //    return Ok(employee);
        //}

        //[HttpPost]
        //public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    var employeeEntity = new Employee
        //    {
        //        Name = addEmployeeDto.Name,
        //        Email = addEmployeeDto.Email,
        //        Phone = addEmployeeDto.Phone,
        //        Salary = addEmployeeDto.Salary
        //    };

        //    var createdEmployee = await _employeeRepository.AddAsync(employeeEntity);
        //    return CreatedAtAction(nameof(GetEmployeeById), new { id = createdEmployee.Id }, createdEmployee);
        //}

        //[HttpPut("{id:int}")]
        //public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    var employee = new Employee
        //    {
        //        Id = id,
        //        Name = updateEmployeeDto.Name,
        //        Email = updateEmployeeDto.Email,
        //        Phone = updateEmployeeDto.Phone,
        //        Salary = updateEmployeeDto.Salary
        //    };

        //    var updatedEmployee = await _employeeRepository.UpdateAsync(id, employee);
        //    if (updatedEmployee == null) return NotFound();
        //    return Ok(updatedEmployee);
        //}

        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> DeleteEmployee(int id)
        //{
        //    var deleted = await _employeeRepository.DeleteAsync(id);
        //    if (!deleted) return NotFound();
        //    return Ok();
        //}

        //[HttpGet("filter-by-salary")]
        //public async Task<IActionResult> GetEmployeesBySalary(decimal minSalary)
        //{
        //    if (minSalary < 0) return BadRequest("Minimum salary cannot be negative");

        //    var employees = await _employeeRepository.GetBySalaryAsync(minSalary);
        //    return Ok(employees);
        //}

        //[HttpGet("search-by-name")]
        //public async Task<IActionResult> SearchEmployeesByName(string name)
        //{
        //    if (string.IsNullOrEmpty(name)) return BadRequest("Name is required");

        //    var employees = await _employeeRepository.SearchByNameAsync(name);
        //    return Ok(employees);
        //}

        //-----------------------------------------------------------------------------

        //private readonly IEmployeeService _employeeService;

        //public EmployeesController(IEmployeeService employeeService)
        //{
        //    _employeeService = employeeService;
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetAllEmployees()
        //{
        //    try
        //    {
        //        var employees = await _employeeService.GetAllEmployeesAsync();
        //        return Ok(employees);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> GetEmployeeById(int id)
        //{
        //    try
        //    {
        //        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        //        if (employee == null) return NotFound();
        //        return Ok(employee);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    try
        //    {
        //        var createdEmployee = await _employeeService.AddEmployeeAsync(addEmployeeDto);
        //        return CreatedAtAction(nameof(GetEmployeeById), new { id = createdEmployee.Id }, createdEmployee);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //[HttpPut("{id:int}")]
        //public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);
        //    if (id != updateEmployeeDto.Id) return BadRequest("ID mismatch");

        //    try
        //    {
        //        var updatedEmployee = await _employeeService.UpdateEmployeeAsync(updateEmployeeDto);
        //        if (updatedEmployee == null) return NotFound();
        //        return Ok(updatedEmployee);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> DeleteEmployee(int id)
        //{
        //    try
        //    {
        //        var deleted = await _employeeService.DeleteEmployeeAsync(id);
        //        if (!deleted) return NotFound();
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //[HttpGet("filter-by-salary")]
        //public async Task<IActionResult> GetEmployeesBySalary(decimal minSalary)
        //{
        //    try
        //    {
        //        var employees = await _employeeService.GetEmployeesBySalaryAsync(minSalary);
        //        return Ok(employees);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //[HttpGet("search-by-name")]
        //public async Task<IActionResult> SearchEmployeesByName(string name)
        //{
        //    try
        //    {
        //        var employees = await _employeeService.SearchEmployeesByNameAsync(name);
        //        return Ok(employees);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //----------------------------------------------------------------------
        //private readonly IMediator _mediator;
        //private readonly ILogger<EmployeesController> _logger; 

        //public EmployeesController(IMediator mediator, ILogger<EmployeesController> logger)
        //{
        //    _mediator = mediator;
        //    _logger = logger;
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetAllEmployees()
        //{
        //    try
        //    {
        //        var query = new GetAllEmployeesQuery();
        //        var employees = await _mediator.Send(query);
        //        return Ok(employees);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi lấy danh sách nhân viên");
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> GetEmployeeById(int id)
        //{
        //    try
        //    {
        //        var query = new GetEmployeeByIdQuery { Id = id };
        //        var employee = await _mediator.Send(query);
        //        if (employee == null) return NotFound();
        //        return Ok(employee);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi lấy nhân viên với ID {Id}", id);
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    try
        //    {
        //        var command = new CreateEmployeeCommand
        //        {
        //            Name = addEmployeeDto.Name,
        //            Email = addEmployeeDto.Email,
        //            Phone = addEmployeeDto.Phone,
        //            Salary = addEmployeeDto.Salary
        //        };
        //        var createdEmployee = await _mediator.Send(command);
        //        return CreatedAtAction(nameof(GetEmployeeById), new { id = createdEmployee.Id }, createdEmployee);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message); // Xử lý lỗi validation
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi thêm nhân viên mới");
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //[HttpPut("{id:int}")]
        //public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    try
        //    {
        //        var command = new UpdateEmployeeCommand
        //        {
        //            Id = id,
        //            Name = updateEmployeeDto.Name,
        //            Email = updateEmployeeDto.Email,
        //            Phone = updateEmployeeDto.Phone,
        //            Salary = updateEmployeeDto.Salary
        //        };
        //        await _mediator.Send(command);
        //        return NoContent();
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi cập nhật nhân viên với ID {Id}", id);
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> DeleteEmployee(int id)
        //{
        //    try
        //    {
        //        var command = new DeleteEmployeeCommand { Id = id };
        //        await _mediator.Send(command);
        //        return NoContent();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi xóa nhân viên với ID {Id}", id);
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //[HttpGet("filter-by-salary")]
        //public async Task<IActionResult> GetEmployeesBySalary(decimal minSalary)
        //{
        //    try
        //    {
        //        var query = new GetEmployeesBySalaryQuery { MinSalary = minSalary };
        //        var employees = await _mediator.Send(query);
        //        return Ok(employees);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi lọc nhân viên theo lương tối thiểu {MinSalary}", minSalary);
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //[HttpGet("search-by-name")]
        //public async Task<IActionResult> SearchEmployeesByName(string name)
        //{
        //    if (string.IsNullOrEmpty(name)) return BadRequest("Tên là bắt buộc");

        //    try
        //    {
        //        var query = new SearchEmployeesByNameQuery { Name = name };
        //        var employees = await _mediator.Send(query);
        //        return Ok(employees);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Lỗi khi tìm kiếm nhân viên theo tên {Name}", name);
        //        return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
        //    }
        //}

        //------------------------------------------------------------------------------
        private readonly IMediator _mediator;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IMediator mediator, ILogger<EmployeesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var query = new GetAllEmployeesQuery();
                var employees = await _mediator.Send(query);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách nhân viên");
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            try
            {
                var query = new GetEmployeeByIdQuery { Id = id };
                var employee = await _mediator.Send(query);
                if (employee == null) return NotFound();
                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy nhân viên với ID {Id}", id);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto addEmployeeDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var command = new CreateEmployeeCommand
                {
                    EmployeeDto = addEmployeeDto
                };
                var createdEmployee = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetEmployeeById), new { id = createdEmployee.Id }, createdEmployee);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi thêm nhân viên");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm nhân viên mới");
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var command = new UpdateEmployeeCommand
                {
                    Id = id,
                    EmployeeDto = updateEmployeeDto
                };
                await _mediator.Send(command);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi cập nhật nhân viên với ID {Id}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật nhân viên với ID {Id}", id);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var command = new DeleteEmployeeCommand { Id = id };
                await _mediator.Send(command);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi xóa nhân viên với ID {Id}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa nhân viên với ID {Id}", id);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpGet("filter-by-salary")]
        public async Task<IActionResult> GetEmployeesBySalary([FromQuery] decimal minSalary)
        {
            try
            {
                var query = new GetEmployeesBySalaryQuery { MinSalary = minSalary };
                var employees = await _mediator.Send(query);
                return Ok(employees);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi lọc nhân viên theo lương tối thiểu {MinSalary}", minSalary);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lọc nhân viên theo lương tối thiểu {MinSalary}", minSalary);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpGet("search-by-name")]
        public async Task<IActionResult> SearchEmployeesByName([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name)) return BadRequest("Tên là bắt buộc");

            try
            {
                var query = new SearchEmployeesByNameQuery { Name = name };
                var employees = await _mediator.Send(query);
                return Ok(employees);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi tìm kiếm nhân viên theo tên {Name}", name);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm nhân viên theo tên {Name}", name);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpPost("{employeeId:int}/jobs")]
        public async Task<IActionResult> AddJob(int employeeId, [FromBody] AddJobDto addJobDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var command = new CreateJobCommand
                {
                    EmployeeId = employeeId,
                    JobDto = addJobDto
                };
                var createdJob = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetJobsByEmployeeId), new { employeeId = createdJob.EmployeeId, jobId = createdJob.Id }, createdJob);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi thêm công việc cho nhân viên với ID {EmployeeId}", employeeId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm công việc cho nhân viên với ID {EmployeeId}", employeeId);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpGet("{employeeId:int}/jobs")]
        public async Task<IActionResult> GetJobsByEmployeeId(int employeeId)
        {
            try
            {
                var query = new GetJobsByEmployeeIdQuery { EmployeeId = employeeId };
                var jobs = await _mediator.Send(query);
                return Ok(jobs);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi lấy công việc của nhân viên với ID {EmployeeId}", employeeId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy công việc của nhân viên với ID {EmployeeId}", employeeId);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpPut("{employeeId:int}/jobs/{jobId:int}")]
        public async Task<IActionResult> UpdateJob(int employeeId, int jobId, [FromBody] UpdateJobDto updateJobDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var command = new UpdateJobCommand
                {
                    JobId = jobId,
                    EmployeeId = employeeId,
                    JobDto = updateJobDto
                };
                await _mediator.Send(command);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi cập nhật công việc {JobId} cho nhân viên với ID {EmployeeId}", jobId, employeeId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật công việc {JobId} cho nhân viên với ID {EmployeeId}", jobId, employeeId);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }

        [HttpDelete("{employeeId:int}/jobs/{jobId:int}")]
        public async Task<IActionResult> DeleteJob(int employeeId, int jobId)
        {
            try
            {
                var command = new DeleteJobCommand { JobId = jobId, EmployeeId = employeeId };
                await _mediator.Send(command);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi xác thực khi xóa công việc {JobId} của nhân viên với ID {EmployeeId}", jobId, employeeId);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa công việc {JobId} của nhân viên với ID {EmployeeId}", jobId, employeeId);
                return StatusCode(500, "Lỗi máy chủ nội bộ: " + ex.Message);
            }
        }
    }
}
