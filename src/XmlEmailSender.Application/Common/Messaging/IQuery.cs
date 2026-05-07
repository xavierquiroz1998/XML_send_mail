using MediatR;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
