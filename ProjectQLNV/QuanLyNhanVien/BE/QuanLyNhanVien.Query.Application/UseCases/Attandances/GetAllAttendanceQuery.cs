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
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize không được vượt quá 100.");
        }
    }

    public class GetAllAttendanceQueryHandler : IRequestHandler<GetAllAttendanceQuery, List<Attendance>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllAttendanceQueryHandler> _logger;

        public GetAllAttendanceQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllAttendanceQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Attendance>> Handle(GetAllAttendanceQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting retrieval of all attendance records with PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber, request.PageSize);

            var validator = new GetAllAttendanceQueryValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for GetAllAttendanceQuery: {Errors}", errorMessages);
                throw new InvalidOperationException(errorMessages);
            }

            try
            {
                var repository = _unitOfWork.Repository<Attendance>();
                var attendances = await repository.GetAll()
                    .Include(a => a.Employee)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Successfully retrieved {Count} attendance records", attendances.Count);
                return attendances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance records with PageNumber: {PageNumber}, PageSize: {PageSize}",
                    request.PageNumber, request.PageSize);
                throw;
            }
        }
    }
}
