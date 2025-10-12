using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Permits {
    public sealed record PermitDeleteRequest(
        [Required] string RowVersion
    );
}
