﻿using FluentValidation;
using MediatR;
using QuanLyNhanVien.Command.Contracts.Errors;
using QuanLyNhanVien.Command.Contracts.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = validationResults.SelectMany(r => r.Errors)
                                               .Where(f => f != null)
                                               .ToList();

                if (failures.Any())
                {
                    var errorMessages = string.Join("; ", failures.Select(f => f.ErrorMessage));
                    if (typeof(TResponse) == typeof(Result<>).MakeGenericType(typeof(TResponse).GenericTypeArguments[0]))
                    {
                        var error = new Error(errorMessages);
                        return (TResponse)(object)Result<object>.Failure(error);
                    }
                    throw new ValidationException(failures);
                }
            }
            return await next();
        }
    }
}
