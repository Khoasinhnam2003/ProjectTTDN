using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Domain.Entities;
using QuanLyNhanVien.Command.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Positions
{
    public record UpdatePositionCommand : IRequest<Result<bool>>
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public string Description { get; set; }
        public decimal? BaseSalary { get; set; }
    }

    public class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdatePositionCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.PositionId)
                .GreaterThan(0).WithMessage("ID vị trí phải lớn hơn 0.");

            RuleFor(x => x.PositionName)
                .NotEmpty().WithMessage("Tên vị trí không được để trống.")
                .MaximumLength(100).WithMessage("Tên vị trí tối đa 100 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Tên vị trí chỉ được chứa chữ cái và khoảng trắng.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.BaseSalary)
                .GreaterThanOrEqualTo(0).WithMessage("Lương cơ bản phải lớn hơn hoặc bằng 0.")
                .When(x => x.BaseSalary.HasValue);

            // Kiểm tra trùng lặp PositionName
            RuleFor(x => x.PositionName)
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    var command = (UpdatePositionCommand)context.InstanceToValidate;
                    var duplicate = await _context.Positions
                        .AnyAsync(p => p.PositionName == name && p.PositionId != command.PositionId, cancellationToken);
                    if (duplicate)
                    {
                        context.AddFailure("Tên vị trí đã tồn tại.");
                    }
                });
        }
    }

    public class UpdatePositionCommandHandler : IRequestHandler<UpdatePositionCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdatePositionCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public UpdatePositionCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdatePositionCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
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

                position.PositionName = request.PositionName;
                position.Description = request.Description;
                position.BaseSalary = request.BaseSalary;
                position.UpdatedAt = DateTime.Now;

                positionRepository.Update(position);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    Console.WriteLine("Thành công");
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật vị trí."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Lỗi khi cập nhật vị trí: {ex.Message}"));
            }
        }
    }
}
