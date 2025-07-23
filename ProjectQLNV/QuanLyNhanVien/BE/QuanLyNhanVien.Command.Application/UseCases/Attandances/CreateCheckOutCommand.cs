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
    public record CreateCheckOutCommand : IRequest<Result<Attendance>>
    {
        public int EmployeeId { get; set; }
    }

    public class CreateCheckOutCommandHandler : IRequestHandler<CreateCheckOutCommand, Result<Attendance>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateCheckOutCommandHandler> _logger;

        public CreateCheckOutCommandHandler(ApplicationDbContext context, IUnitOfWork unitOfWork, ILogger<CreateCheckOutCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Attendance>> Handle(CreateCheckOutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing check-out for EmployeeId: {EmployeeId}", request.EmployeeId);

            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1).AddSeconds(-1);
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId &&
                                        a.CheckInTime >= todayStart &&
                                        a.CheckInTime <= todayEnd &&
                                        !a.CheckOutTime.HasValue,
                                        cancellationToken);

            if (existingAttendance == null)
            {
                _logger.LogWarning("No pending check-in found for EmployeeId: {EmployeeId}", request.EmployeeId);
                return Result<Attendance>.Failure(new Error("Không tìm thấy bản ghi check-in chưa hoàn thành trong ngày."));
            }

            existingAttendance.CheckOutTime = DateTime.Now;
            existingAttendance.Status = "Absent";
            existingAttendance.UpdatedAt = DateTime.Now;

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var repository = _unitOfWork.Repository<Attendance, int>();
                repository.Update(existingAttendance);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _unitOfWork.Commit();
                transaction.Dispose();

                var updatedAttendance = await _context.Attendances
                    .Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.AttendanceId == existingAttendance.AttendanceId, cancellationToken);

                _logger.LogInformation("Check-out successful for EmployeeId: {EmployeeId} at {CheckOutTime}", request.EmployeeId, existingAttendance.CheckOutTime);
                return Result<Attendance>.Success(updatedAttendance);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                transaction.Dispose();
                _logger.LogError(ex, "Check-out failed for EmployeeId: {EmployeeId}", request.EmployeeId);
                return Result<Attendance>.Failure(new Error($"Lỗi khi cập nhật check-out: {ex.Message}"));
            }
        }
    }
}
