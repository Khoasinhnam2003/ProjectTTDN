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

namespace QuanLyNhanVien.Query.Application.UseCases.Positions
{
    public class GetAllPositionsQuery : IRequest<List<Position>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllPositionsQueryValidator : AbstractValidator<GetAllPositionsQuery>
    {
        public GetAllPositionsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");
        }
    }

    public class GetAllPositionsQueryHandler : IRequestHandler<GetAllPositionsQuery, List<Position>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllPositionsQueryHandler> _logger;

        public GetAllPositionsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllPositionsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Position>> Handle(GetAllPositionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetAllPositionsQuery with PageNumber={PageNumber}, PageSize={PageSize}", request.PageNumber, request.PageSize);
            try
            {
                var repository = _unitOfWork.Repository<Position>();
                var positions = await repository.GetAll()
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
                _logger.LogInformation("Retrieved {Count} positions", positions.Count);
                return positions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetAllPositionsQuery");
                throw;
            }
        }
    }
}
