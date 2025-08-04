using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DeleteSkillCommandHandler> _logger;

        public DeleteSkillCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DeleteSkillCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new DeleteSkillCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting deletion of skill with ID: {SkillId}", request.SkillId);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for skill ID {SkillId}: {Errors}", request.SkillId, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var skillRepository = _unitOfWork.Repository<Skill, int>();
            var skill = await skillRepository.FindAsync(request.SkillId, cancellationToken);
            if (skill == null)
            {
                _logger.LogWarning("Skill with ID {SkillId} not found", request.SkillId);
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
                    _logger.LogInformation("Successfully deleted skill with ID: {SkillId}", request.SkillId);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when deleting skill with ID: {SkillId}", request.SkillId);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi xóa kỹ năng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                {
                    _logger.LogWarning("Cannot delete skill with ID {SkillId} due to foreign key constraint", request.SkillId);
                    return Result<bool>.Failure(new Error("Không thể xóa kỹ năng vì có ràng buộc khóa ngoại."));
                }
                _logger.LogError(ex, "Error deleting skill with ID: {SkillId}", request.SkillId);
                return Result<bool>.Failure(new Error($"Lỗi khi xóa kỹ năng: {ex.Message}"));
            }
        }
    }
}
