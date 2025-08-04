using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Application.UseCases.Attandances
{
    public class CalculateWorkHoursQuery : IRequest<double>
    {
        public int AttendanceId { get; set; }
    }

    public class CalculateWorkHoursQueryValidator : AbstractValidator<CalculateWorkHoursQuery>
    {
        public CalculateWorkHoursQueryValidator()
        {
            RuleFor(x => x.AttendanceId)
                .GreaterThan(0).WithMessage("AttendanceId phải lớn hơn 0.");
        }
    }

    public class CalculateWorkHoursQueryHandler : IRequestHandler<CalculateWorkHoursQuery, double>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CalculateWorkHoursQueryHandler> _logger;

        public CalculateWorkHoursQueryHandler(IUnitOfWork unitOfWork, ILogger<CalculateWorkHoursQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<double> Handle(CalculateWorkHoursQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting calculation of work hours for AttendanceId: {AttendanceId}", request.AttendanceId);

            var validator = new CalculateWorkHoursQueryValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for CalculateWorkHoursQuery for AttendanceId {AttendanceId}: {Errors}",
                    request.AttendanceId, errorMessages);
                throw new InvalidOperationException(errorMessages);
            }

            try
            {
                var repository = _unitOfWork.Repository<Attendance>();
                var attendance = await repository.GetAll()
                    .FirstOrDefaultAsync(a => a.AttendanceId == request.AttendanceId, cancellationToken)
                    ?? throw new InvalidOperationException("Bản ghi chấm công không tồn tại.");

                if (!attendance.CheckOutTime.HasValue)
                {
                    _logger.LogWarning("Cannot calculate work hours for AttendanceId {AttendanceId}: CheckOutTime is not set",
                        request.AttendanceId);
                    throw new ArgumentException("Không thể tính giờ làm việc: CheckOutTime chưa được thiết lập.");
                }

                var workSpan = attendance.CheckOutTime.Value - attendance.CheckInTime;
                var workHours = workSpan.TotalHours;
                _logger.LogInformation("Successfully calculated work hours for AttendanceId {AttendanceId}: {WorkHours} hours",
                    request.AttendanceId, workHours);
                return workHours;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating work hours for AttendanceId: {AttendanceId}", request.AttendanceId);
                throw;
            }
        }
    }
}
