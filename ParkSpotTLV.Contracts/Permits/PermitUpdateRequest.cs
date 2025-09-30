using ParkSpotTLV.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Permits {
    public sealed record PermitUpdateRequest(

        [Required] string RowVersion,
        PermitType Type,
        int? ZoneCode

    );

}
