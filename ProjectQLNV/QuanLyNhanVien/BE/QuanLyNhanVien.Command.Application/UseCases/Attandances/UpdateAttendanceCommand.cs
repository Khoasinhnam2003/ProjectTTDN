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
    public record UpdateAttendanceCommand : IRequest<Result<Attendance>>
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateAttendanceCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.AttendanceId)
                .GreaterThan(0).WithMessage("AttendanceId phải lớn hơn 0.")
                .MustAsync(async (id, cancellation) =>
                {
                    return await _context.Attendances.AnyAsync(a => a.AttendanceId == id, cancellation);
                }).WithMessage("Bản ghi điểm danh không tồn tại.");

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId phải lớn hơn 0.")
                .MustAsync(async (id, cancellation) =>
                {
                    return await _context.Employees.AnyAsync(e => e.EmployeeId == id, cancellation);
                }).WithMessage("Nhân viên không tồn tại.");

            RuleFor(x => x.CheckInTime)
                .NotEmpty().WithMessage("CheckInTime không được để trống.")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("CheckInTime không được trong tương lai.");

            RuleFor(x => x.CheckOutTime)
                .GreaterThanOrEqualTo(x => x.CheckInTime).When(x => x.CheckOutTime.HasValue)
                .WithMessage("CheckOutTime phải lớn hơn hoặc bằng CheckInTime.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Trạng thái không được để trống.")
                .Must(status => new[] { "Present", "Absent", "Leave", "Late", "EarlyLeave" }.Contains(status))
                .WithMessage("Trạng thái không hợp lệ. Các giá trị hợp lệ: Present, Absent, Leave, Late, EarlyLeave.");

            RuleFor(x => x.Notes)
                .MaximumLength(200).WithMessage("Ghi chú không được vượt quá 200 ký tự.")
                .When(x => x.Notes != null);

            RuleFor(x => x).CustomAsync(async (command, context, cancellation) =>
            {
                var startOfDay = command.CheckInTime.Date;
                var endOfDay = startOfDay.AddDays(1);
                var existingAttendance = await _context.Attendances
                    .AnyAsync(a => a.EmployeeId == command.EmployeeId
                        && a.CheckInTime >= startOfDay
                        && a.CheckInTime < endOfDay
                        && a.AttendanceId != command.AttendanceId, cancellation);
                if (existingAttendance)
                {
                    context.AddFailure("Đã có bản ghi điểm danh khác cho nhân viên này trong ngày được chọn.");
                }
            });
        }
    }

    public class UpdateAttendanceCommandHandler : IRequestHandler<UpdateAttendanceCommand, Result<Attendance>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UpdateAttendanceCommandHandler> _logger;

        public UpdateAttendanceCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<UpdateAttendanceCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Attendance>> Handle(UpdateAttendanceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting update for attendance with ID: {AttendanceId}", request.AttendanceId);

            var validator = new UpdateAttendanceCommandValidator(_context);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for attendance ID {AttendanceId}: {Errors}", request.AttendanceId, errorMessages);
                return Result<Attendance>.Failure(new Error(errorMessages));
            }

            var repository = _unitOfWork.Repository<Attendance, int>();
            var attendance = await repository.FindAsync(request.AttendanceId, cancellationToken);
            if (attendance == null)
            {
                _logger.LogWarning("Attendance with ID {AttendanceId} not found", request.AttendanceId);
                return Result<Attendance>.Failure(new Error("Bản ghi điểm danh không tồn tại."));
            }

            attendance.EmployeeId = request.EmployeeId;
            attendance.CheckInTime = request.CheckInTime;
            attendance.CheckOutTime = request.CheckOutTime;
            attendance.Status = request.Status;
            attendance.Notes = request.Notes;
            attendance.UpdatedAt = DateTime.Now;

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                repository.Update(attendance);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _unitOfWork.Commit();
                transaction.Dispose();

                var updatedAttendance = await _context.Attendances
                    .Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId, cancellationToken);

                _logger.LogInformation("Successfully updated attendance with ID: {AttendanceId}", request.AttendanceId);
                return Result<Attendance>.Success(updatedAttendance);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                transaction.Dispose();
                _logger.LogError(ex, "Error updating attendance with ID: {AttendanceId}", request.AttendanceId);
                return Result<Attendance>.Failure(new Error($"Lỗi khi cập nhật điểm danh: {ex.Message}"));
            }
        }
    }
}
