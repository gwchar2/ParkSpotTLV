using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Permits {
    public sealed record PermitDeleteRequest(
        Guid Id,
        [Required] string RowVersion
    );
}
