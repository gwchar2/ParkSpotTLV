namespace ParkSpotTLV.Contracts.Enums;

/* 
 * Days-of-week flags
 * We use flags so we can store (Mon|Tue|Wed|Thu|Fri) in a single int.
 */
[System.Flags]
public enum DaysOfWeekMask {
    Sun = 1, Mon = 2, Tue = 4, Wed = 8, Thu = 16, Fri = 32, Sat = 64,
    All = Sun | Mon | Tue | Wed | Thu | Fri | Sat,
    Weekdays = Mon | Tue | Wed | Thu | Fri,
    Weekend = Sat | Sun
}

/*
 * A segment's instantaneous status (after evaluating time + overrides),
 * prior to applying user preferences (showPaid, showFree).
 */
public enum SegmentStatus {
    Free = 1,               // Legal without payment at this time
    Paid = 2,               // Legal but requires payment at this time
    Forbidden = 3           // Not legal at this time
}

/*
 * Tariff group windows are always "Paid" (outside them is Free),
 */
public enum SegmentWindowKind {
    Paid = 1,
    Forbidden = 2
}

public enum SegmentSide { 
    Both = 0, 
    Left = 1, 
    Right = 2 
}