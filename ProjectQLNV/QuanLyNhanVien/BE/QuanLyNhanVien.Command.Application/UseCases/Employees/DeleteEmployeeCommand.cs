using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;


namespace QuanLyNhanVien.Command.Application.UseCases.Employees
{
    public record DeleteEmployeeCommand : IRequest<Result<bool>>
    {
        public int EmployeeId { get; set; }
    }

    public class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteEmployeeCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("EmployeeId không được để trống.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
                    return employee != null;
                }).WithMessage("Nhân viên với ID đã cho không tồn tại.");
        }
    }

    public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteEmployeeCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

        public DeleteEmployeeCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteEmployeeCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteEmployeeCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of employee with ID: {EmployeeId}", request.EmployeeId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for employee ID {EmployeeId}: {Errors}", request.EmployeeId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var employeeRepository = _unitOfWork.Repository<Employee, int>();
            var employee = await employeeRepository.FindAsync(request.EmployeeId, cancellationToken);

            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found", request.EmployeeId);
                return Result<bool>.Failure(new Error("Nhân viên không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                employeeRepository.Delete(employee);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully deleted employee with ID: {EmployeeId}", request.EmployeeId);
                    return Result<bool>.Success(true);
                }
                transaction.Rollback();
                _logger.LogWarning("No changes made when deleting employee with ID: {EmployeeId}", request.EmployeeId);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa nhân viên."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error deleting employee with ID: {EmployeeId}", request.EmployeeId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa nhân viên: {ex.Message}"));
            }
        }
    }
}
