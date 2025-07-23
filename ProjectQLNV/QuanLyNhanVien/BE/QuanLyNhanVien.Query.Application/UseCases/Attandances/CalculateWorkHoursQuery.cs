using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
                .GreaterThan(0).WithMessage("AttendanceId must be greater than 0.");
        }
    }

    public class CalculateWorkHoursQueryHandler : IRequestHandler<CalculateWorkHoursQuery, double>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CalculateWorkHoursQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<double> Handle(CalculateWorkHoursQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Attendance>();
            var attendance = await repository.GetAll()
                .FirstOrDefaultAsync(a => a.AttendanceId == request.AttendanceId, cancellationToken)
                ?? throw new InvalidOperationException("Attendance record not found.");

            if (!attendance.CheckOutTime.HasValue)
            {
                throw new ArgumentException("Cannot calculate work hours: CheckOutTime is not set.");
            }

            var workSpan = attendance.CheckOutTime.Value - attendance.CheckInTime;
            return workSpan.TotalHours;
        }
    }
}
