using MediatR;
using Microsoft.AspNetCore.Mvc;
using XmlEmailSender.API.Common;
using XmlEmailSender.API.Contracts.Smtp;
using XmlEmailSender.Application.Smtp.Commands.DeleteSmtpConfiguration;
using XmlEmailSender.Application.Smtp.Commands.SaveSmtpConfiguration;
using XmlEmailSender.Application.Smtp.Queries.ListSmtpConfigurations;

namespace XmlEmailSender.API.Controllers;

[ApiController]
[Route("api/smtp-config")]
public sealed class SmtpConfigController : ControllerBase
{
    private readonly IMediator _mediator;
    public SmtpConfigController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSmtpConfigurationsQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveSmtpRequest body, CancellationToken ct)
    {
        var cmd = new SaveSmtpConfigurationCommand(
            Id: null,
            body.Name, body.Host, body.Port, body.UseSsl,
            body.Username, body.Password,
            body.FromEmail, body.FromName, body.Activate);

        var result = await _mediator.Send(cmd, ct);
        return result.ToCreatedResult(dto => $"/api/smtp-config/{dto.Id}");
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SaveSmtpRequest body,
        CancellationToken ct)
    {
        var cmd = new SaveSmtpConfigurationCommand(
            Id: id,
            body.Name, body.Host, body.Port, body.UseSsl,
            body.Username, body.Password,
            body.FromEmail, body.FromName, body.Activate);

        var result = await _mediator.Send(cmd, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSmtpConfigurationCommand(id), ct);
        return result.ToActionResult();
    }
}
