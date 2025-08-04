using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.Skills
{
    public record CreateSkillCommand : IRequest<Result<Skill>>
    {
        public int EmployeeId { get; set; }
        public string SkillName { get; set; }
        public string ProficiencyLevel { get; set; }
        public string Description { get; set; }
    }

    public class CreateSkillCommandValidator : AbstractValidator<CreateSkillCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateSkillCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

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

            RuleFor(x => x).CustomAsync(async (command, context, cancellationToken) =>
            {
                var skill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.EmployeeId == command.EmployeeId && s.SkillName == command.SkillName, cancellationToken);
                if (skill != null)
                {
                    context.AddFailure("Kỹ năng này đã tồn tại cho nhân viên này.");
                }
            });
        }
    }

    public class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, Result<Skill>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateSkillCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateSkillCommandHandler> _logger;

        public CreateSkillCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<CreateSkillCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateSkillCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Skill>> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting creation of skill for employee ID: {EmployeeId}, SkillName: {SkillName}",
                request.EmployeeId, request.SkillName);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for skill creation for employee ID {EmployeeId}, SkillName: {SkillName}: {Errors}",
                    request.EmployeeId, request.SkillName, errorMessages);
                return Result<Skill>.Failure(new Error(errorMessages));
            }

            var skill = new Skill
            {
                EmployeeId = request.EmployeeId,
                SkillName = request.SkillName,
                ProficiencyLevel = request.ProficiencyLevel,
                Description = request.Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var skillRepository = _unitOfWork.Repository<Skill, int>();
                skillRepository.Add(skill);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully created skill with ID: {SkillId} for employee ID: {EmployeeId}, SkillName: {SkillName}",
                        skill.SkillId, request.EmployeeId, request.SkillName);
                    return Result<Skill>.Success(skill);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when creating skill for employee ID: {EmployeeId}, SkillName: {SkillName}",
                    request.EmployeeId, request.SkillName);
                return Result<Skill>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo kỹ năng."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error creating skill for employee ID: {EmployeeId}, SkillName: {SkillName}",
                    request.EmployeeId, request.SkillName);
                return Result<Skill>.Failure(new Error($"Lỗi khi tạo kỹ năng: {ex.Message}"));
            }
        }
    }
}
