﻿using FluentValidation;
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
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.UseCases.SalaryHistories
{
    public record CreateSalaryHistoryCommand : IRequest<Result<SalaryHistory>>
    {
        public int EmployeeId { get; set; }
        public decimal Salary { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class CreateSalaryHistoryCommandValidator : AbstractValidator<CreateSalaryHistoryCommand>
    {
        private readonly ApplicationDbContext _context;

        public CreateSalaryHistoryCommandValidator(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId phải lớn hơn 0.")
                .MustAsync(async (id, cancellationToken) =>
                {
                    var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
                    return employee != null;
                }).WithMessage("Nhân viên với ID đã cho không tồn tại.");

            RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Lương phải lớn hơn 0.");

            RuleFor(x => x.EffectiveDate)
                .NotEmpty().LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày hiệu lực không được trong tương lai.");
        }
    }

    public class CreateSalaryHistoryCommandHandler : IRequestHandler<CreateSalaryHistoryCommand, Result<SalaryHistory>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateSalaryHistoryCommandValidator _validator;
        private readonly ApplicationDbContext _context;

        public CreateSalaryHistoryCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validator = new CreateSalaryHistoryCommandValidator(context);
        }

        public async Task<Result<SalaryHistory>> Handle(CreateSalaryHistoryCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<SalaryHistory>.Failure(new Error(errorMessages));
            }

            var salaryHistory = new SalaryHistory
            {
                EmployeeId = request.EmployeeId,
                Salary = request.Salary,
                EffectiveDate = request.EffectiveDate,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var repository = _unitOfWork.Repository<SalaryHistory, int>();
                repository.Add(salaryHistory);
                int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (changes > 0)
                {
                    transaction.Commit();
                    var createdSalaryHistory = await _context.SalaryHistories
                        .Include(sh => sh.Employee)
                        .FirstOrDefaultAsync(sh => sh.SalaryHistoryId == salaryHistory.SalaryHistoryId, cancellationToken);
                    return Result<SalaryHistory>.Success(createdSalaryHistory);
                }
                transaction.Rollback();
                return Result<SalaryHistory>.Failure(new Error("Không có thay đổi nào được thực hiện khi tạo lịch sử lương."));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result<SalaryHistory>.Failure(new Error($"Lỗi khi tạo lịch sử lương: {ex.Message}"));
            }
        }
    }
}
