using FluentValidation;
using MediatR;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Positions
{
    public record DeletePositionCommand : IRequest<Result<bool>>
    {
        public int PositionId { get; set; }
    }

    public class DeletePositionCommandValidator : AbstractValidator<DeletePositionCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeletePositionCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.PositionId)
                .NotEmpty().WithMessage("ID vị trí không được để trống.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var position = await _context.Positions.FindAsync(new object[] { id }, cancellationToken);
                    return position != null;
                }).WithMessage("Vị trí với ID đã cho không tồn tại.");
        }
    }

    public class DeletePositionCommandHandler : IRequestHandler<DeletePositionCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeletePositionCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public DeletePositionCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeletePositionCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(DeletePositionCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<bool>.Failure(new Error(errorMessages));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var positionRepository = _unitOfWork.Repository<Position, int>();
                var position = await positionRepository.FindAsync(request.PositionId, cancellationToken);
                if (position == null)
                {
                    transaction.Rollback();
                    return Result<bool>.Failure(new Error("Vị trí không tồn tại."));
                }

                positionRepository.Delete(position);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    Console.WriteLine("Thành công");
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa vị trí."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                {
                    return Result<bool>.Failure(new Error("Không thể xóa vị trí vì có nhân viên đang giữ vị trí này."));
                }
                return Result<bool>.Failure(new Error($"Lỗi khi xóa vị trí: {ex.Message}"));
            }
        }
    }
}
