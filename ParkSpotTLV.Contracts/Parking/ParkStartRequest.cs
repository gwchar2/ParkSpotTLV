using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkSpotTLV.Contracts.Parking {
    public sealed record ParkStartRequest (
                    DateTimeOffset serverUtc,
                    DateTimeOffset? startedAtUtc,
                    DateTimeOffset? freeParkingUntilUtc,
                    int remainingSeconds,
                    int budgetMinutes
                );
}
