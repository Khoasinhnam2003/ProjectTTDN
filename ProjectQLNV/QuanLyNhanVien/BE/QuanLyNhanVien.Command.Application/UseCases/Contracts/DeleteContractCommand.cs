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
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Contracts
{
    public record DeleteContractCommand : IRequest<Result<bool>>
    {
        public int ContractId { get; set; }
    }

    public class DeleteContractCommandValidator : AbstractValidator<DeleteContractCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteContractCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.ContractId)
                .GreaterThan(0).WithMessage("ContractId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var contract = await _context.Contracts.FindAsync(new object[] { id }, cancellationToken);
                    return contract != null;
                }).WithMessage("Hợp đồng với ID đã cho không tồn tại.");
        }
    }

    public class DeleteContractCommandHandler : IRequestHandler<DeleteContractCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteContractCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public DeleteContractCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteContractCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(DeleteContractCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var contractRepository = _unitOfWork.Repository<Contract, int>();
            var contract = await contractRepository.FindAsync(request.ContractId, cancellationToken);
            if (contract == null)
            {
                return Result<bool>.Failure(new Error("Hợp đồng không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                contractRepository.Delete(contract);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    return Result<bool>.Success(true);
                }
                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa hợp đồng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Lỗi khi xóa hợp đồng: {ex.Message}"));
            }
        }
    }
}
