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

namespace QuanLyNhanVien.Command.Application.UseCases.SalaryHistories
{
    public record DeleteSalaryHistoryCommand : IRequest<Result<bool>>
    {
        public int SalaryHistoryId { get; set; }
    }

    public class DeleteSalaryHistoryCommandValidator : AbstractValidator<DeleteSalaryHistoryCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteSalaryHistoryCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.SalaryHistoryId)
                .NotEmpty().WithMessage("SalaryHistoryId không được để trống.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var salaryHistory = await _context.SalaryHistories.FindAsync(new object[] { id }, cancellationToken);
                    return salaryHistory != null;
                }).WithMessage("Lịch sử lương với ID đã cho không tồn tại.");
        }
    }

    public class DeleteSalaryHistoryCommandHandler : IRequestHandler<DeleteSalaryHistoryCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteSalaryHistoryCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteSalaryHistoryCommandHandler> _logger;

        public DeleteSalaryHistoryCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteSalaryHistoryCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteSalaryHistoryCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteSalaryHistoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for salary history ID {SalaryHistoryId}: {Errors}",
                    request.SalaryHistoryId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var repository = _unitOfWork.Repository<SalaryHistory, int>();
            var salaryHistory = await repository.FindAsync(request.SalaryHistoryId, cancellationToken);

            if (salaryHistory == null)
            {
                _logger.LogWarning("Salary history with ID {SalaryHistoryId} not found", request.SalaryHistoryId);
                return Result<bool>.Failure(new Error("Lịch sử lương không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                repository.Delete(salaryHistory);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully deleted salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when deleting salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa lịch sử lương."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error deleting salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa lịch sử lương: {ex.Message}"));
            }
        }
    }
}
