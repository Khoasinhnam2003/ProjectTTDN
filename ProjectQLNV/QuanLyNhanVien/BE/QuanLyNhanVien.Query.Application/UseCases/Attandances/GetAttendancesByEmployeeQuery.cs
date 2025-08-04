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
    public class GetAttendancesByEmployeeQuery : IRequest<List<Attendance>>
    {
        public int EmployeeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }

    public class GetAttendancesByEmployeeQueryValidator : AbstractValidator<GetAttendancesByEmployeeQuery>
    {
        public GetAttendancesByEmployeeQueryValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId phải lớn hơn 0.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize không được vượt quá 100.");
        }
    }

    public class GetAttendancesByEmployeeQueryHandler : IRequestHandler<GetAttendancesByEmployeeQuery, List<Attendance>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAttendancesByEmployeeQueryHandler> _logger;

        public GetAttendancesByEmployeeQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAttendancesByEmployeeQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Attendance>> Handle(GetAttendancesByEmployeeQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting retrieval of attendance records for EmployeeId: {EmployeeId}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.EmployeeId, request.PageNumber, request.PageSize);

            var validator = new GetAttendancesByEmployeeQueryValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for GetAttendancesByEmployeeQuery for EmployeeId {EmployeeId}: {Errors}",
                    request.EmployeeId, errorMessages);
                throw new InvalidOperationException(errorMessages);
            }

            try
            {
                var repository = _unitOfWork.Repository<Attendance>();
                var attendances = await repository.GetAll()
                    .Include(a => a.Employee)
                    .Where(a => a.EmployeeId == request.EmployeeId)
                    .OrderBy(a => a.AttendanceId)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                if (!attendances.Any())
                {
                    _logger.LogWarning("No attendance records found for EmployeeId: {EmployeeId}", request.EmployeeId);
                    throw new InvalidOperationException("Không tìm thấy bản ghi chấm công cho nhân viên này.");
                }

                _logger.LogInformation("Successfully retrieved {Count} attendance records for EmployeeId: {EmployeeId}",
                    attendances.Count, request.EmployeeId);
                return attendances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance records for EmployeeId: {EmployeeId}", request.EmployeeId);
                throw;
            }
        }
    }
}
