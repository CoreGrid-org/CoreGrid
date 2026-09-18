namespace CoreGrid.Api.Features.Maintenance.DTOs;

public class UploadPhotoResponse
{
    // FR-034: despite the name (kept as-is so the frontend's existing
    // pass-through — feed this straight into ReportFaultRequest/
    // CreateMaintenanceRequest.PhotoUrl — needs no change), this is now an
    // opaque R2 object key, not a directly-usable URL. It only becomes a
    // real, time-limited URL when the owning record is read back through
    // an authorized GetById/list call.
    public required string Url { get; set; }
}
