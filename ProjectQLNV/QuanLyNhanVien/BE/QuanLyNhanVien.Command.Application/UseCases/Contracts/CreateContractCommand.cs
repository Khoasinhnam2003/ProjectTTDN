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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Contracts
{
    public record CreateContractCommand : IRequest<Result<Contract>>
    {
        public int EmployeeId { get; set; }
        public string ContractType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Salary { get; set; }
        public string Status { get; set; }
    }

    public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateContractCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
                    return employee != null;
                }).WithMessage("Nhân viên với ID đã cho không tồn tại.");

            RuleFor(x => x.ContractType)
                .NotEmpty().WithMessage("Loại hợp đồng không được để trống.")
                .MaximumLength(50).WithMessage("Loại hợp đồng tối đa 50 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Loại hợp đồng chỉ được chứa chữ cái và khoảng trắng.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày bắt đầu không được trong tương lai.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue)
                .WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

            RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Mức lương phải lớn hơn 0.");

            RuleFor(x => x.Status)
                .MaximumLength(50).WithMessage("Trạng thái tối đa 50 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Status));
        }
    }

    public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, Result<Contract>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateContractCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateContractCommandHandler> _logger;

        public CreateContractCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<CreateContractCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateContractCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Contract>> Handle(CreateContractCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting creation of contract for EmployeeId: {EmployeeId}", request.EmployeeId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for EmployeeId {EmployeeId}: {Errors}", request.EmployeeId, errorMessages);
                return Result<Contract>.Failure(new Error(errorMessages));
            }

            var contract = new Contract
            {
                EmployeeId = request.EmployeeId,
                ContractType = request.ContractType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Salary = request.Salary,
                Status = request.Status,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var contractRepository = _unitOfWork.Repository<Contract, int>();
                contractRepository.Add(contract);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully created contract for EmployeeId: {EmployeeId} with ID: {ContractId}",
                        request.EmployeeId, contract.ContractId);
                    return Result<Contract>.Success(contract);
                }
                transaction.Rollback();
                _logger.LogWarning("No changes made when creating contract for EmployeeId: {EmployeeId}", request.EmployeeId);
                return Result<Contract>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo hợp đồng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error creating contract for EmployeeId: {EmployeeId}", request.EmployeeId);
                return Result<Contract>.Failure(new Error($"Lỗi khi tạo hợp đồng: {ex.Message}"));
            }
        }
    }
}
