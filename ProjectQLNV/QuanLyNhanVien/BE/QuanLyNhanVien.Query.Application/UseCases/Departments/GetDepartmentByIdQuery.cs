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

namespace QuanLyNhanVien.Query.Application.UseCases.Departments
{
    public class GetDepartmentByIdQuery : IRequest<Department>
    {
        public int DepartmentId { get; set; }
    }

    public class GetDepartmentByIdQueryValidator : AbstractValidator<GetDepartmentByIdQuery>
    {
        public GetDepartmentByIdQueryValidator()
        {
            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("DepartmentId must be greater than 0.");
        }
    }

    public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, Department>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDepartmentByIdQueryHandler> _logger;

        public GetDepartmentByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetDepartmentByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Department> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetDepartmentByIdQuery for DepartmentId: {DepartmentId}", request.DepartmentId);
            try
            {
                var repository = _unitOfWork.Repository<Department>();
                var department = await repository.GetAll()
                    .Include(d => d.Manager)
                    .FirstOrDefaultAsync(d => d.DepartmentId == request.DepartmentId, cancellationToken);

                if (department == null)
                {
                    _logger.LogWarning("Department with ID: {DepartmentId} not found", request.DepartmentId);
                    throw new InvalidOperationException("Department not found.");
                }

                _logger.LogInformation("Successfully retrieved department with ID: {DepartmentId}", request.DepartmentId);
                return department;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetDepartmentByIdQuery for DepartmentId: {DepartmentId}", request.DepartmentId);
                throw;
            }
        }
    }
}
