Notes & Pipeline Mapping

Query stage: ISegmentQueryService → pulls only fields needed into SegmentSnapshot.

Strategy stage: ITariffCalendarService, IPrivilegedPolicyService, IPriceDecisionService make isolated decisions (easy to test/swap).

Logic stage: IAvailabilityService builds time horizons; IClassificationService maps to UI groups.

Specification stage: IMinDurationSpec enforces the “at least X minutes legal” eligibility; ILimitedSpec and IPrivilegedIllegalSpec are available if you want to plug them directly into classification/filters elsewhere.

Facade: IMapSegmentsEvaluator coordinates the steps and emits SegmentResult[].