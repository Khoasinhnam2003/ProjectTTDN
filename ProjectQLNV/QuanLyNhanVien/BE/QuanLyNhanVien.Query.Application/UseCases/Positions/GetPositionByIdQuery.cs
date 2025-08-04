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
    public class GetPositionByIdQuery : IRequest<Position>
    {
        public int PositionId { get; set; }
    }

    public class GetPositionByIdQueryValidator : AbstractValidator<GetPositionByIdQuery>
    {
        public GetPositionByIdQueryValidator()
        {
            RuleFor(x => x.PositionId)
                .GreaterThan(0).WithMessage("PositionId must be greater than 0.");
        }
    }

    public class GetPositionByIdQueryHandler : IRequestHandler<GetPositionByIdQuery, Position>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPositionByIdQueryHandler> _logger;

        public GetPositionByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetPositionByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Position> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetPositionByIdQuery for PositionId={PositionId}", request.PositionId);
            try
            {
                var repository = _unitOfWork.Repository<Position>();
                var position = await repository.GetAll()
                    .FirstOrDefaultAsync(p => p.PositionId == request.PositionId, cancellationToken);

                if (position == null)
                {
                    _logger.LogWarning("Position with ID {PositionId} not found", request.PositionId);
                    throw new InvalidOperationException("Position not found.");
                }

                _logger.LogInformation("Successfully retrieved position with ID {PositionId}", request.PositionId);
                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling GetPositionByIdQuery for PositionId={PositionId}", request.PositionId);
                throw;
            }
        }
    }
}
