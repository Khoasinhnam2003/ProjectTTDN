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

namespace QuanLyNhanVien.Command.Application.UseCases.Skills
{
    public record DeleteSkillCommand : IRequest<Result<bool>>
    {
        public int SkillId { get; set; }
    }

    public class DeleteSkillCommandValidator : AbstractValidator<DeleteSkillCommand>
    {
        private readonly ApplicationDbContext _context;

        public DeleteSkillCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.SkillId)
                .GreaterThan(0).WithMessage("SkillId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var skill = await _context.Skills.FindAsync(new object[] { id }, cancellationToken);
                    return skill != null;
                }).WithMessage("Kỹ năng với ID đã cho không tồn tại.");
        }
    }

    public class DeleteSkillCommandHandler : IRequestHandler<DeleteSkillCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DeleteSkillCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public DeleteSkillCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteSkillCommandValidator(context);
        }

        public async Task<Result<bool>> Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var skillRepository = _unitOfWork.Repository<Skill, int>();
            var skill = await skillRepository.FindAsync(request.SkillId, cancellationToken);
            if (skill == null)
            {
                return Result<bool>.Failure(new Error("Kỹ năng không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                skillRepository.Delete(skill);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    return Result<bool>.Success(true);
                }
                transaction.Rollback();
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa kỹ năng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<bool>.Failure(new Error($"Lỗi khi xóa kỹ năng: {ex.Message}"));
            }
        }
    }
}
