using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    public record CreateAttendanceCommand : IRequest<Result<Attendance>>
    {
        public int EmployeeId { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public bool IsAutoCheckIn { get; set; } // Thêm flag để xác định check-in tự động
    }

    public class CreateAttendanceCommandValidator : AbstractValidator<CreateAttendanceCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateAttendanceCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

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
                .WithMessage("CheckOutTime phải lớn hơn hoặc bằng CheckInTime.")
                .LessThanOrEqualTo(DateTime.Now).When(x => x.CheckOutTime.HasValue)
                .WithMessage("CheckOutTime không được trong tương lai.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Trạng thái không được để trống.")
                .Must(status => new[] { "Present", "Absent", "Leave", "Late", "EarlyLeave" }.Contains(status))
                .WithMessage("Trạng thái không hợp lệ. Các giá trị hợp lệ: Present, Absent, Leave, Late, EarlyLeave.");

            RuleFor(x => x.Notes)
                .MaximumLength(200).WithMessage("Ghi chú không được vượt quá 200 ký tự.")
                .When(x => x.Notes != null);

            // Chỉ kiểm tra trùng lặp nếu không phải check-in tự động
            RuleFor(x => x).CustomAsync(async (command, context, cancellation) =>
            {
                if (!command.IsAutoCheckIn)
                {
                    var startOfDay = command.CheckInTime.Date;
                    var endOfDay = startOfDay.AddDays(1);
                    var existingAttendance = await _context.Attendances
                        .AnyAsync(a => a.EmployeeId == command.EmployeeId
                            && a.CheckInTime >= startOfDay
                            && a.CheckInTime < endOfDay, cancellation);
                    if (existingAttendance)
                    {
                        context.AddFailure("Đã có bản ghi điểm danh cho nhân viên này trong ngày được chọn.");
                    }
                }
            });
        }
    }

    public class CreateAttendanceCommandHandler : IRequestHandler<CreateAttendanceCommand, Result<Attendance>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public CreateAttendanceCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<Attendance>> Handle(CreateAttendanceCommand request, CancellationToken cancellationToken)
        {
            var validator = new CreateAttendanceCommandValidator(_context);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<Attendance>.Failure(new Error(errorMessages));
            }

            var attendance = new Attendance
            {
                EmployeeId = request.EmployeeId,
                CheckInTime = request.CheckInTime,
                CheckOutTime = request.CheckOutTime,
                Status = request.Status,
                Notes = request.Notes,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var repository = _unitOfWork.Repository<Attendance, int>();
                repository.Add(attendance);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _unitOfWork.Commit();
                transaction.Dispose();

                var createdAttendance = await _context.Attendances
                    .Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId, cancellationToken);

                return Result<Attendance>.Success(createdAttendance);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                transaction.Dispose();
                return Result<Attendance>.Failure(new Error($"Lỗi khi thêm điểm danh: {ex.Message}"));
            }
        }
    }
}
