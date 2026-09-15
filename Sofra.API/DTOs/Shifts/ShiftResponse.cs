namespace Sofra.API.DTOs.Shifts;

public record ShiftResponse(
    int Id, int EmployeeId, int UserId, string UserName,
    DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string? Note);
