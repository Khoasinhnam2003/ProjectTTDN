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

namespace QuanLyNhanVien.Command.Application.UseCases.Departments
{
    public record CreateDepartmentCommand : IRequest<Result<bool>>
    {
        public string DepartmentName { get; set; }
        public string Location { get; set; }
        public int? ManagerId { get; set; }
    }

    public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateDepartmentCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.DepartmentName)
                .NotEmpty().WithMessage("Tên phòng ban không được để trống.")
                .MaximumLength(100).WithMessage("Tên phòng ban tối đa 100 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s]+$")).WithMessage("Tên phòng ban chỉ được chứa chữ cái và khoảng trắng.");

            RuleFor(x => x.Location)
                .MaximumLength(100).WithMessage("Địa điểm tối đa 100 ký tự.")
                .Matches(new Regex("^[\\p{L}\\s0-9,.-]+$")).When(x => !string.IsNullOrEmpty(x.Location))
                .WithMessage("Địa điểm chỉ được chứa chữ cái, số, khoảng trắng, dấu phẩy, dấu chấm và dấu gạch ngang.");

            RuleFor(x => x.DepartmentName)
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    var duplicate = await _context.Departments
                        .AnyAsync(d => d.DepartmentName == name, cancellationToken);
                    if (duplicate)
                    {
                        context.AddFailure("Tên phòng ban đã tồn tại.");
                    }
                });

            RuleFor(x => x.ManagerId)
                .CustomAsync(async (managerId, context, cancellationToken) =>
                {
                    if (managerId.HasValue)
                    {
                        var employee = await _context.Employees
                            .FindAsync(new object[] { managerId.Value }, cancellationToken);
                        if (employee == null)
                        {
                            context.AddFailure("Nhân viên được chỉ định làm quản lý không tồn tại.");
                        }
                    }
                });
        }
    }

    public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateDepartmentCommandValidator _validator;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateDepartmentCommandHandler> _logger;

        public CreateDepartmentCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<CreateDepartmentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateDepartmentCommandValidator(context);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting creation of department with name: {DepartmentName}", request.DepartmentName);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for department name {DepartmentName}: {Errors}", request.DepartmentName, errorMessages);
                return Result<bool>.Failure(new Error(errorMessages));
            }

            var department = new Department
            {
                DepartmentName = request.DepartmentName,
                Location = request.Location,
                ManagerId = request.ManagerId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var departmentRepository = _unitOfWork.Repository<Department, int>();
                departmentRepository.Add(department);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    _logger.LogInformation("Successfully created department with name: {DepartmentName}", request.DepartmentName);
                    return Result<bool>.Success(true);
                }

                transaction.Rollback();
                _logger.LogWarning("No changes made when creating department with name: {DepartmentName}", request.DepartmentName);
                return Result<bool>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo phòng ban."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error creating department with name: {DepartmentName}", request.DepartmentName);
                return Result<bool>.Failure(new Error($"Lỗi khi tạo phòng ban: {ex.Message}"));
            }
        }
    }
}
