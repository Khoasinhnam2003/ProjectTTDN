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
    public class GetDepartmentEmployeeCountQuery : IRequest<int>
    {
        public int DepartmentId { get; set; }
    }

    public class GetDepartmentEmployeeCountQueryValidator : AbstractValidator<GetDepartmentEmployeeCountQuery>
    {
        public GetDepartmentEmployeeCountQueryValidator()
        {
            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("DepartmentId must be greater than 0.");
        }
    }

    public class GetDepartmentEmployeeCountQueryHandler : IRequestHandler<GetDepartmentEmployeeCountQuery, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger _logger;

        public GetDepartmentEmployeeCountQueryHandler(IUnitOfWork unitOfWork, ILogger logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> Handle(GetDepartmentEmployeeCountQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetDepartmentEmployeeCountQuery for DepartmentId: {DepartmentId}", request.DepartmentId);
            try
            {
                var employeeRepository = _unitOfWork.Repository<Employee>();
                var count = await employeeRepository.GetAll()
                    .CountAsync(e => e.DepartmentId == request.DepartmentId, cancellationToken);
                _logger.LogInformation("Retrieved employee count: {EmployeeCount} for DepartmentId: {DepartmentId}", count, request.DepartmentId);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetDepartmentEmployeeCountQuery for DepartmentId: {DepartmentId}", request.DepartmentId);
                throw;
            }
        }
    }
}
