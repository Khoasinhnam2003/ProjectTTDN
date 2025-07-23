using FluentValidation;
using MediatR;
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
    public record UpdateContractCommand : IRequest<Result<Contract>>
    {
        public int ContractId { get; set; }
        public int EmployeeId { get; set; }
        public string ContractType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Salary { get; set; }
        public string Status { get; set; }
    }

    public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateContractCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.ContractId)
                .GreaterThan(0).WithMessage("ContractId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var contract = await _context.Contracts.FindAsync(new object[] { id }, cancellationToken);
                    return contract != null;
                }).WithMessage("Hợp đồng với ID đã cho không tồn tại.");

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

    public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand, Result<Contract>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateContractCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public UpdateContractCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateContractCommandValidator(context);
        }

        public async Task<Result<Contract>> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<Contract>.Failure(new Error(errorMessages));
            }

            var contractRepository = _unitOfWork.Repository<Contract, int>();
            var contract = await contractRepository.FindAsync(request.ContractId, cancellationToken);
            if (contract == null)
            {
                return Result<Contract>.Failure(new Error("Hợp đồng không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                contract.EmployeeId = request.EmployeeId;
                contract.ContractType = request.ContractType;
                contract.StartDate = request.StartDate;
                contract.EndDate = request.EndDate;
                contract.Salary = request.Salary;
                contract.Status = request.Status;
                contract.UpdatedAt = DateTime.Now;

                contractRepository.Update(contract);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    return Result<Contract>.Success(contract);
                }
                transaction.Rollback();
                return Result<Contract>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật hợp đồng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<Contract>.Failure(new Error($"Lỗi khi cập nhật hợp đồng: {ex.Message}"));
            }
        }
    }
}
