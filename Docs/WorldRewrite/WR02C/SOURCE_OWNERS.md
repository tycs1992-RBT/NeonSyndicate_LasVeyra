# Source owners

- `VisibleRealityV0.WorldRewrite.cs` owns the existing EXPLORE buttons and now selects the appropriate existing resolver.
- `CityLayout.ResolveRealmAnchor04OC` remains the authority for Realm destinations. The new route calls it; it does not change layout or navigation geometry.
- `CampaignPlacesRuntime04E` remains the live authority for Venue and other 04E tokens.
- `Gps` still owns waypoint state, road planning, mission precedence and clearing the waypoint.
- The existing quest runner, quest data, quest prerequisites and reward ledger remain unchanged.
- `KingdomWR02BTests.cs` contains an additional route test for the real revision-10 layout. That test was not executed here.
- The prior source contract remains exact-byte scoped. Its reviewed route entry now includes this precise route change; the existing mutation tests still reject partial and extra edits.
