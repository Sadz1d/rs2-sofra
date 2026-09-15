using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sofra.API.Constants;
using Sofra.API.DTOs;
using Sofra.API.DTOs.Reservations;
using Sofra.API.Extensions;
using Sofra.API.Requests.Reservations;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController(IReservationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationListItemResponse>>> GetList([FromQuery] ReservationListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetListAsync(request, User.GetUserId(), IsStaff(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("availability")]
    public async Task<ActionResult<IReadOnlyList<ReservationSlotResponse>>> GetAvailability([FromQuery] ReservationAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await service.GetAvailabilityAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, User.GetUserId(), User.GetRoles(), IsStaff(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> Create(ReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, User.GetUserId(), User.GetRoles(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<ReservationResponse>> ChangeStatus(int id, ReservationTransitionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.TransitionAsync(id, request, User.GetUserId(), User.GetRoles(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/table")]
    [Authorize(Roles = $"{Roles.Konobar},{Roles.Admin}")]
    public async Task<ActionResult<ReservationResponse>> AssignTable(int id, AssignReservationTableRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignTableAsync(id, request, User.GetUserId(), User.GetRoles(), cancellationToken);
        return Ok(result);
    }

    private bool IsStaff() => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Konobar) || User.IsInRole(Roles.Kuhar);
}
