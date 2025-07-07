using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public DeleteEmployeeCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteEmployeeCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<bool>.Failure(new Error(errorMessages));
            }
            var employeeRepository = _unitOfWork.Repository<Employee, int>();
            var employee = await employeeRepository.FindAsync(request.EmployeeId, cancellationToken);

            if (employee == null)
            {
                return Result<bool>.Failure(new Error("Nhân viên không tồn tại."));
            }
            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                employeeRepository.Delete(employee);
                int changes = await employeeRepository.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    return Result<bool>.Success(true);
                }
                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa nhân viên."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Lỗi khi xóa nhân viên: {ex.Message}"));
            }
        }
    }
}
