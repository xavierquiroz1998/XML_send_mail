using MediatR;
using XmlEmailSender.Domain.Common;

namespace XmlEmailSender.Application.Common.Messaging;

/// <summary>
/// Command que NO devuelve datos (solo Result éxito/fallo).
/// </summary>
public interface ICommand : IRequest<Result> { }

/// <summary>
/// Command que devuelve un valor (típicamente un DTO o Id) en caso de éxito.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
