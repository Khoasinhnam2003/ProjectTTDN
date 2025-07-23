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

        public GetAttendancesByEmployeeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<Attendance>> Handle(GetAttendancesByEmployeeQuery request, CancellationToken cancellationToken)
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
                throw new InvalidOperationException("Không tìm thấy bản ghi chấm công cho nhân viên này.");
            }

            return attendances;
        }
    }
}
