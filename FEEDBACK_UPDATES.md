# Updates made in response to Part 1 and Part 2 feedback

## Part 1 feedback

No marker feedback is recorded for Part 1 in this document.

## Part 2 feedback

| Criterion | Feedback received | Change made | Where |
|---|---|---|---|
| User Interface Design Considerations (8/10) | "I need to see more engagement." and "Nice design but I need to see more into this application like the home page, about us in relation to the project etc." | Redesigned the **Home page** into a full landing page that explains the project: a hero section, a live "what your fleet is doing right now" summary, the three-pillar overview, a "how a single reading travels" walkthrough, an operator-control section and the technology stack. Added a dedicated **About page** that describes the Smart-X project and its approach (what it is, the environments it serves, how signals become a system), linked from the navigation menu and the home page. Upgraded the **telemetry dashboard** so it is more engaging and informative: a KPI strip (fleet health, Normal / Warning / Anomalous counts), trend sparklines, delta indicators and threshold colour coding. Added `enhance.css` to give the new pages consistent typography, colours and layout. | `style(client)` commit `9b60c4e` (19 Oct): `Home.razor`, `About.razor`, `Dashboard.razor`, `NavMenu.razor`, `wwwroot/enhance.css` |
| Predictive Action and Suggestion Engine (20/30) | "I would like to see some more insightful suggestions." | Suggestions were extended beyond a single "this command was used N times" line. In Part 3 the system now gives **failover advice** when a node goes down: it lists the affected devices, the reason each is down, and the alternative hops that keep them connected (`DownNodeAdviceDto` / `HopAlternativeDto`), shown as advice cards on the new Network Topology page. Command Stream and Dashboard device cards now link straight to **Route & history** for that sensor, so an operator can act on a suggestion with the route and the historical readings in front of them. | `feat(api)` commit `db8d8f8` (topology service and failover advice), `feat(shared)` commit `98f6e20` (advice DTOs), `style(client)` commit `d2e9f13` (advice cards), `feat(client)` commits `b9f21b8` and `ae8de89` (Route & history links), all 10 Nov |

## How Part 1 and Part 2 were carried forward into Part 3

- The Part 2 `DeviceIndex` (MAC lookup) is reused so the route tracker accepts a MAC address.
- The Part 1 `Disconnected` telemetry status now drives whether a node counts as down in the mesh.
- The Part 2 navigation pattern (disabled pillar, then enabled) was reused to switch on the third menu option.
- The Part 2 home page and navigation menu were updated again in Part 3 (`3fd29f6`, `64e1196`) so the Network Topology pillar is live and linked from every entry point, as part of keeping the home page informative.

## Additional fix (not marker-driven)

- Fixed a `KeyNotFoundException` on first render of the Command Stream page by replacing the per-device dictionary indexer with a `HashSet` for the critical toggle. `fix(client)` commit `f0310ee` (11 Oct).
