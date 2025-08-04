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

namespace QuanLyNhanVien.Command.Application.UseCases.Attandances
{
    public record DeleteAttendanceCommand : IRequest<Result<bool>>
    {
        public int AttendanceId { get; set; }
    }

    public class DeleteAttendanceCommandValidator : AbstractValidator<DeleteAttendanceCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteAttendanceCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.AttendanceId)
                .GreaterThan(0).WithMessage("AttendanceId phải lớn hơn 0.")
                .MustAsync(async (id, cancellation) =>
                {
                    return await _context.Attendances.AnyAsync(a => a.AttendanceId == id, cancellation);
                }).WithMessage("Bản ghi điểm danh không tồn tại.");
        }
    }

    public class DeleteAttendanceCommandHandler : IRequestHandler<DeleteAttendanceCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteAttendanceCommandHandler> _logger;

        public DeleteAttendanceCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteAttendanceCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteAttendanceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of attendance with ID: {AttendanceId}", request.AttendanceId);

            var validator = new DeleteAttendanceCommandValidator(_context);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for attendance ID {AttendanceId}: {Errors}", request.AttendanceId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var repository = _unitOfWork.Repository<Attendance, int>();
            var attendance = await repository.FindAsync(request.AttendanceId, cancellationToken);
            if (attendance == null)
            {
                _logger.LogWarning("Attendance with ID {AttendanceId} not found", request.AttendanceId);
                return Result<bool>.Failure(new Error("Bản ghi điểm danh không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                repository.Delete(attendance);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _unitOfWork.Commit();
                transaction.Dispose();

                _logger.LogInformation("Successfully deleted attendance with ID: {AttendanceId}", request.AttendanceId);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                transaction.Dispose();
                _logger.LogError(ex, "Error deleting attendance with ID: {AttendanceId}", request.AttendanceId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa điểm danh: {ex.Message}"));
            }
        }
    }
}
