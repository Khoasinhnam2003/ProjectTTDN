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

        public GetPositionByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Position> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.Repository<Position>();
            var position = await repository.GetAll()
                .FirstOrDefaultAsync(p => p.PositionId == request.PositionId, cancellationToken);

            if (position == null)
            {
                throw new InvalidOperationException("Position not found.");
            }

            return position;
        }
    }
}
