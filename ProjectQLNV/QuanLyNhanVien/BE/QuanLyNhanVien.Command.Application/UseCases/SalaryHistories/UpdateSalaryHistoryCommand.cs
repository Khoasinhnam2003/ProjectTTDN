using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    public record UpdateSalaryHistoryCommand : IRequest<Result<SalaryHistory>>
    {
        public int SalaryHistoryId { get; set; }
        public decimal Salary { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class UpdateSalaryHistoryCommandValidator : AbstractValidator<UpdateSalaryHistoryCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateSalaryHistoryCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.SalaryHistoryId)
                .GreaterThan(0).WithMessage("SalaryHistoryId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var salaryHistory = await _context.SalaryHistories.FindAsync(new object[] { id }, cancellationToken);
                    return salaryHistory != null;
                }).WithMessage("Lịch sử lương với ID đã cho không tồn tại.");

            RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Lương phải lớn hơn 0.");

            RuleFor(x => x.EffectiveDate)
                .NotEmpty().LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày hiệu lực không được trong tương lai.");
        }
    }

    public class UpdateSalaryHistoryCommandHandler : IRequestHandler<UpdateSalaryHistoryCommand, Result<SalaryHistory>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateSalaryHistoryCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UpdateSalaryHistoryCommandHandler> _logger;

        public UpdateSalaryHistoryCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<UpdateSalaryHistoryCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateSalaryHistoryCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<SalaryHistory>> Handle(UpdateSalaryHistoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting update for salary history ID: {SalaryHistoryId}, Salary: {Salary}, EffectiveDate: {EffectiveDate}",
                request.SalaryHistoryId, request.Salary, request.EffectiveDate);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for salary history ID {SalaryHistoryId}: {Errors}",
                    request.SalaryHistoryId, errorMessages);
                return Result<SalaryHistory>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var repository = _unitOfWork.Repository<SalaryHistory, int>();
                var salaryHistory = await repository.FindAsync(request.SalaryHistoryId, cancellationToken);
                if (salaryHistory == null)
                {
                    transaction.Rollback();
                    _logger.LogWarning("Salary history with ID {SalaryHistoryId} not found", request.SalaryHistoryId);
                    return Result<SalaryHistory>.Failure(new Error("Lịch sử lương không tồn tại."));
                }

                salaryHistory.Salary = request.Salary;
                salaryHistory.EffectiveDate = request.EffectiveDate;
                salaryHistory.UpdatedAt = DateTime.Now; // 01:45 PM +07, 30/07/2025

                repository.Update(salaryHistory);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    var updatedSalaryHistory = await _context.SalaryHistories
                        .Include(sh => sh.Employee)
                        .FirstOrDefaultAsync(sh => sh.SalaryHistoryId == salaryHistory.SalaryHistoryId, cancellationToken);
                    _logger.LogInformation("Successfully updated salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);
                    return Result<SalaryHistory>.Success(updatedSalaryHistory);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when updating salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);
                return Result<SalaryHistory>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật lịch sử lương."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error updating salary history with ID: {SalaryHistoryId}", request.SalaryHistoryId);
                return Result<SalaryHistory>.Failure(new Error($"Lỗi khi cập nhật lịch sử lương: {ex.Message}"));
            }
        }
    }
}
