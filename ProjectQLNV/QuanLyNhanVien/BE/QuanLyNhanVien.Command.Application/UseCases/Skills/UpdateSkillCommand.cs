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

namespace QuanLyNhanVien.Command.Application.UseCases.Skills
{
    public record UpdateSkillCommand : IRequest<Result<Skill>>
    {
        public int SkillId { get; set; }
        public int EmployeeId { get; set; }
        public string SkillName { get; set; }
        public string ProficiencyLevel { get; set; }
        public string Description { get; set; }
    }

    public class UpdateSkillCommandValidator : AbstractValidator<UpdateSkillCommand>
    {
        private readonly ApplicationDbContext _context;

        public UpdateSkillCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.SkillId)
                .GreaterThan(0).WithMessage("SkillId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var skill = await _context.Skills.FindAsync(new object[] { id }, cancellationToken);
                    return skill != null;
                }).WithMessage("Kỹ năng với ID đã cho không tồn tại.");

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
                    return employee != null;
                }).WithMessage("Nhân viên với ID đã cho không tồn tại.");

            RuleFor(x => x.SkillName)
                .NotEmpty().WithMessage("Tên kỹ năng không được để trống.")
                .MaximumLength(100).WithMessage("Tên kỹ năng tối đa 100 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Tên kỹ năng chỉ được chứa chữ cái và khoảng trắng.");

            RuleFor(x => x.ProficiencyLevel)
                .MaximumLength(50).WithMessage("Mức độ thành thạo tối đa 50 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.ProficiencyLevel));

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Mô tả tối đa 200 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Kiểm tra trùng lặp SkillName cho cùng EmployeeId
            RuleFor(x => x).CustomAsync(async (command, context, cancellationToken) =>
            {
                var skill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.EmployeeId == command.EmployeeId && s.SkillName == command.SkillName && s.SkillId != command.SkillId, cancellationToken);
                if (skill != null)
                {
                    context.AddFailure("Kỹ năng này đã tồn tại cho nhân viên này.");
                }
            });
        }
    }

    public class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, Result<Skill>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpdateSkillCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public UpdateSkillCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new UpdateSkillCommandValidator(context);
        }

        public async Task<Result<Skill>> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<Skill>.Failure(new Error(errorMessages));
            }

            var skillRepository = _unitOfWork.Repository<Skill, int>();
            var skill = await skillRepository.FindAsync(request.SkillId, cancellationToken);
            if (skill == null)
            {
                return Result<Skill>.Failure(new Error("Kỹ năng không tồn tại."));
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                skill.EmployeeId = request.EmployeeId;
                skill.SkillName = request.SkillName;
                skill.ProficiencyLevel = request.ProficiencyLevel;
                skill.Description = request.Description;
                skill.UpdatedAt = DateTime.Now;

                skillRepository.Update(skill);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    return Result<Skill>.Success(skill);
                }
                transaction.Rollback();
                return Result<Skill>.Failure(new Error("Không có thay đổi nào được thực hiện khi cập nhật kỹ năng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<Skill>.Failure(new Error($"Lỗi khi cập nhật kỹ năng: {ex.Message}"));
            }
        }
    }
}
