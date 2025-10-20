namespace ParkSpotTLV.Infrastructure.Auth.Models {
    public enum RefreshTokenStatus { 
        Active, 
        Expired, 
        Revoked, 
        NotFound, 
        Reused 
    }
}
