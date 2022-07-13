namespace OCPP.Core.WebAPI.Dtos;

public class ChargePointDto
{
    public string ChargePointId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Comment { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? ClientCertThumb { get; set; }
    public string? Status { get; set; } = null!;
}