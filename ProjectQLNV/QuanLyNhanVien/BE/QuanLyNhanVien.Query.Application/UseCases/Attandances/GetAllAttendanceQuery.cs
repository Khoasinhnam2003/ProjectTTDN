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
    public class GetAllAttendanceQuery : IRequest<List<Attendance>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }

    public class GetAllAttendanceQueryValidator : AbstractValidator<GetAllAttendanceQuery>
    {
        public GetAllAttendanceQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllAttendanceQueryHandler : IRequestHandler<GetAllAttendanceQuery, List<Attendance>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllAttendanceQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<Attendance>> Handle(GetAllAttendanceQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Attendance>();
            return await repository.GetAll()
                .Include(a => a.Employee)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }
    }
}
