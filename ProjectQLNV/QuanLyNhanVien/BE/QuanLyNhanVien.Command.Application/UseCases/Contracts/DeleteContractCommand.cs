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
        private readonly ILogger<DeleteContractCommandHandler> _logger;

        public DeleteContractCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteContractCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteContractCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteContractCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of contract with ID: {ContractId}", request.ContractId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for contract ID {ContractId}: {Errors}", request.ContractId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var contractRepository = _unitOfWork.Repository<Contract, int>();
            var contract = await contractRepository.FindAsync(request.ContractId, cancellationToken);
            if (contract == null)
            {
                _logger.LogWarning("Contract with ID {ContractId} not found", request.ContractId);
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
                    _logger.LogInformation("Successfully deleted contract with ID: {ContractId}", request.ContractId);
                    return Result<bool>.Success(true);
                }
                transaction.Rollback();
                _logger.LogWarning("No changes made when deleting contract with ID: {ContractId}", request.ContractId);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa hợp đồng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error deleting contract with ID: {ContractId}", request.ContractId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa hợp đồng: {ex.Message}"));
            }
        }
    }
}
