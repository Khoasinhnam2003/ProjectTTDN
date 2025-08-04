using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Employees
{
    public record UpdateEmployeeSalaryCommand : IRequest<Result<bool>>
    {
        public int EmployeeId { get; set; }
        public decimal NewSalary { get; set; }
    }

    public class UpdateEmployeeSalaryCommandValidator : AbstractValidator<UpdateEmployeeSalaryCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateEmployeeSalaryCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId must be greater than 0.");
            RuleFor(x => x.NewSalary)
                .GreaterThan(0).WithMessage("New salary must be greater than 0.");
        }
    }

    public class UpdateEmployeeSalaryCommandHandler : IRequestHandler<UpdateEmployeeSalaryCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateEmployeeSalaryCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UpdateEmployeeSalaryCommandHandler> _logger;

        public UpdateEmployeeSalaryCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<UpdateEmployeeSalaryCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateEmployeeSalaryCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(UpdateEmployeeSalaryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting salary update for employee ID: {EmployeeId} with new salary: {NewSalary}", request.EmployeeId, request.NewSalary);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for employee ID {EmployeeId}: {Errors}", request.EmployeeId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var employeeRepository = _unitOfWork.Repository<Employee, int>();
                var employee = await employeeRepository.FindAsync(request.EmployeeId, cancellationToken);
                if (employee == null)
                {
                    transaction.Rollback();
                    _logger.LogWarning("Employee with ID {EmployeeId} not found", request.EmployeeId);
                    return Result<bool>.Failure(new Error("Employee not found."));
                }

                var salaryHistory = new SalaryHistory
                {
                    EmployeeId = request.EmployeeId,
                    Salary = request.NewSalary,
                    EffectiveDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                var salaryHistoryRepository = _unitOfWork.Repository<SalaryHistory, int>();
                salaryHistoryRepository.Add(salaryHistory);

                employee.UpdatedAt = DateTime.Now;
                employeeRepository.Update(employee);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully updated salary for employee ID: {EmployeeId} to {NewSalary}", request.EmployeeId, request.NewSalary);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when updating salary for employee ID: {EmployeeId}", request.EmployeeId);
                return Result<bool>.Failure(new Error("No changes were made when updating employee salary."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error updating salary for employee ID: {EmployeeId}", request.EmployeeId);
                return Result<bool>.Failure(new Error($"Error updating employee salary: {ex.Message}"));
            }
        }
    }
}
