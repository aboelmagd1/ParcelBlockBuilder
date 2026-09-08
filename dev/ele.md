
Enhance the existing ParcelBlockBuilder ArcGIS Pro Add-in by improving the
"Electric Room / Substation" step.

IMPORTANT:
Do not rebuild the existing workflow or rewrite unrelated code.
Inspect the existing project architecture, models, geometry services, preview
services, map overlay implementation, and UI patterns first.
Reuse the existing services, styles, MVVM structure, BlockConfiguration, and
geometry engine wherever possible.

The Electric Room step must be implemented as a real spatial configuration
feature, not just a form for entering width and depth.

==================================================

1. WORKFLOW POSITION
   ==================================================

Add the Electric Room / Substation step immediately after:

7. Corner & Chamfer

and before:

9. Alignment

The final workflow should become:

1. Base Dimensions
2. Relationship
3. Arrangement
4. Parcel Count & Preview
5. Similarity
6. Exceptions
7. Corner & Chamfer
8. Electric Room / Substation
9. Alignment
10. Dynamic Preview
11. Validation Rules
12. Final Review & Output

Renumber the existing steps automatically without breaking navigation.

==================================================
2. PURPOSE
==========

The Electric Room represents a small rectangular electrical room /
substation that is placed along a street frontage.

The user must be able to:

- Enable or disable the Electric Room.
- Define its dimensions.
- Select the street/side where it will be located.
- Select its exact placement interactively.
- Place it entirely inside one parcel OR across the boundary between two
  adjacent parcels.
- Automatically generate its footprint.
- Automatically clip/carve the affected parcel geometries.
- Preview the result in both the WPF schematic preview and the ArcGIS Pro map.
- Validate that the resulting geometry is valid and remains inside the block.

The source parcels must NEVER be modified.

The result must be generated as new geometry.

==================================================
3. ELECTRIC ROOM CONFIGURATION
==============================

Create/reuse an ElectricRoomConfiguration model.

It should contain at least:

- Enabled
- Width
- Depth
- PlacementSide
- PlacementMethod
- AnchorPoint
- HostParcelIds
- PlacementType
- ClipHostParcels
- Geometry
- ValidationStatus

Use appropriate enums instead of string values where practical.

PlacementType should support:

- InsideSingleParcel
- BetweenTwoParcels

PlacementMethod should support:

- InteractiveMapPlacement
- OptionalOffsetPlacement only if the existing architecture benefits from it

The primary/default workflow must be Interactive Map Placement.

==================================================
4. DIMENSIONS
=============

The UI must provide:

Width Along Street (m)
Depth Into Parcel (m)

Example:

Width Along Street = 2.50 m
Depth Into Parcel = 5.00 m

Calculate the area automatically:

Area = Width × Depth

Example:

2.50 × 5.00 = 12.50 m²

Area must be read-only.

The terminology is important:

"Width Along Street"
"Depth Into Parcel"

Do not use ambiguous Width / Height terminology.

==================================================
5. STREET / SIDE SELECTION
==========================

Allow the user to select the street frontage where the Electric Room
will be placed.

For example:

Placement Street

○ Side A / Street A
○ Side B / Street B

Use the actual side/street naming already used by the project if available.

The selected side must control the orientation of the Electric Room.

The room must be aligned with the street frontage, not simply with the
global map coordinate system.

==================================================
6. INTERACTIVE PLACEMENT
========================

The main placement method must allow the user to select the location
directly on the ArcGIS Pro map.

The user clicks a location on the selected street frontage.

The tool must then:

1. Detect the nearest valid street/frontage segment.
2. Determine the local street direction.
3. Determine the inward direction into the block.
4. Determine the host parcel or parcels.
5. Generate the Electric Room footprint using the configured dimensions.
6. Display the footprint as a temporary preview.
7. Show the affected parcel(s).
8. Allow the user to accept or change the location.

The clicked point should be treated as an anchor/location reference,
not necessarily as the center of the final rectangle.

==================================================
7. GEOMETRY ORIENTATION
=======================

This is critical.

The Electric Room must be oriented relative to the selected street.

"Width Along Street" must follow the local street/frontage direction.

"Depth Into Parcel" must extend perpendicular to the frontage and toward
the interior of the block.

Do NOT assume that the street is horizontal or vertical.

The logic must work with rotated and irregular block geometries.

Use the existing geometry engine / ArcGIS GeometryEngine where appropriate.

==================================================
8. CASE A — INSIDE ONE PARCEL
==============================

If the selected location results in the Electric Room being entirely
inside one parcel:

Example:

Street
────────────────────────

┌──────────────────────┐
│          ER       │                                  │
│────────┘                                  │

│                                                         │
│                                                         │
└──────────────────────┘

The system must:

- Identify the host parcel.
- Generate the Electric Room polygon.
- Subtract the Electric Room footprint from the host parcel.
- Keep the remaining parcel as a valid polygon.
- Keep the Electric Room as a separate polygon.

Use polygon Difference/Clip operations through the existing geometry
architecture.

==================================================
9. CASE B — BETWEEN TWO PARCELS
================================

If the selected location is on or crosses the boundary between two
adjacent parcels:

Example:

Street


The system must:

- Detect both adjacent parcels.
- Store both parcel IDs as HostParcelIds.
- Generate one Electric Room polygon.
- Split/clip the affected portion from Parcel A.
- Split/clip the affected portion from Parcel B.
- Preserve the remaining geometry of both parcels.
- Keep the Electric Room as a separate polygon.

Do not duplicate the Electric Room geometry.

The Electric Room must remain one logical feature even when it occupies
parts of two parcels.

==================================================
10. IMPORTANT GEOMETRY RULE
===========================

The Electric Room must not extend outside the block boundary.

Before accepting placement:

Check:

ElectricRoomGeometry ⊆ BlockGeometry

If it exceeds the block boundary:

Show an ERROR:

"Electric Room cannot be placed here because the configured footprint
extends outside the block boundary."

Do not accept invalid placement.

Also validate:

- Valid polygon geometry.
- Non-zero area.
- Width and depth > 0.
- Host parcel(s) exist.
- Room intersects the intended host parcel(s).
- Room is connected to the selected street frontage.
- No unexpected overlap with unrelated parcels.
- No self-intersections.

==================================================
11. MAP PREVIEW
===============

The Electric Room must be displayed as temporary preview geometry
on the ArcGIS Pro map.

The preview must NOT create a permanent Feature Class or modify
source data.

Use the existing Map Overlay / GraphicsLayer / temporary visualization
architecture already present in the project.

The map preview should show:

- Electric Room footprint.
- Selected host parcel(s).
- Placement anchor.
- Optional dimension indicators.
- Validation status.

When the user changes:

- Width
- Depth
- Street
- Placement location

the map preview must update immediately.

==================================================
12. SCHEMATIC PREVIEW
=====================

Update the existing Dynamic Preview to display the Electric Room.

Use the same geometry/configuration logic as the map preview.

Example:

Street A
────────────────────────────

┌───┬───┬───┬───────┬───┐
│ 1 │ 2 │ 3 │  ER   │ 5 │
└───┴───┴───┴───────┴───┘

The Electric Room should be clearly distinguishable from normal parcels.

Show:

- ER label
- Dimensions
- Host parcel relationship
- Placement status

The schematic preview must update dynamically.

==================================================
13. UI / UX
===========

Follow the existing ArcGIS Pro visual language and project styling.

Do not introduce a completely different UI style.

Recommended layout:

[✓] Include Electric Room / Substation

Dimensions
----------

Width Along Street (m)     [2.50]
Depth Into Parcel (m)      [5.00]
Substation Area             [12.50 m²]

Placement Street
----------------

○ Side A / Street A
○ Side B / Street B

Placement
---------

[ Select Location on Map ]

Status
------

✓ Valid placement
Host Parcel: 05
Placement: Inside Parcel

OR:

✓ Valid placement
Host Parcels: 05, 06
Placement: Between Two Parcels

The "Select Location on Map" action should activate the map interaction
and clearly indicate that the user is selecting the Electric Room location.

==================================================
14. INTERACTIVE MAP STATE
=========================

When the user clicks "Select Location on Map":

- Activate placement mode.
- Change cursor/tool state if supported by ArcGIS Pro SDK.
- Show a temporary anchor/preview.
- Highlight the detected host parcel(s).
- Show the Electric Room footprint.
- Allow the user to move/reselect the location.
- Provide a clear way to finish/cancel placement.

Do not leave the map tool in an active state after the operation is complete.

==================================================
15. HOST PARCEL DETECTION
=========================

Do not rely only on the clicked point.

Use the generated Electric Room footprint to determine the actual
affected parcel(s).

The algorithm should distinguish:

Case 1:
Footprint intersects only one parcel
→ InsideSingleParcel

Case 2:
Footprint intersects two adjacent parcels and crosses their boundary
→ BetweenTwoParcels

Case 3:
Footprint intersects unrelated/multiple parcels
→ Invalid placement

This is important because the clicked anchor alone is not enough to
determine the final relationship.

==================================================
16. CLIPPING / CARVING
======================

When:

ClipHostParcels = true

generate the resulting parcel geometries using polygon difference:

RemainingParcel = OriginalParcel - ElectricRoomFootprint

For two host parcels:

RemainingParcelA = ParcelA - ElectricRoomFootprint
RemainingParcelB = ParcelB - ElectricRoomFootprint

Never modify the original source feature class.

All operations must work on in-memory geometry.

==================================================
17. FINAL OUTPUT
================

When the user clicks Generate Feature Class:

Generate:

- Remaining parcel polygons.
- Electric Room / Substation polygon as a separate feature.

If the project already has an output schema, extend it consistently.

Recommended attributes for Electric Room:

FeatureType = "ElectricRoom"
Width
Depth
Area
PlacementType
HostParcel1
HostParcel2
PlacementSide

For normal parcels:

FeatureType = "Parcel"

Do not break the existing output schema.

==================================================
18. VALIDATION
==============

Add specific Electric Room validation rules.

ERROR:

- Dimensions are zero/negative.
- No placement selected.
- Footprint outside block.
- Invalid geometry.
- Unexpected parcel overlap.
- Cannot determine host parcel.
- More than two host parcels detected.
- Room does not connect to the selected street frontage.

WARNING:

- Room is very close to a block corner.
- Room significantly changes the host parcel geometry.
- Placement is close to another special configuration.

INFO:

- Room occupies one parcel.
- Room crosses two parcels.

==================================================
19. PERFORMANCE
===============

Do not run expensive GIS operations on the UI thread.

Use ArcGIS Pro SDK threading patterns correctly.

Preview operations should be lightweight and responsive.

Do not repeatedly query the entire feature class for every mouse movement.

Use spatial filtering / candidate parcel filtering where appropriate.

==================================================
20. ARCHITECTURE
================

Do not implement the Electric Room logic directly inside the WPF code-behind.

Use the existing architecture.

Prefer:

ElectricRoomConfiguration
        ↓
ElectricRoomService / PlacementService
        ↓
Geometry Engine
        ↓
Validation
        ↓
Preview
        ↓
Final Output

Reuse existing BlockConfiguration and Geometry services.

The Electric Room must become part of the single source of truth.

==================================================
21. IMPORTANT ACCEPTANCE CRITERIA
=================================

The implementation is complete only when all of the following work:

1. User enables Electric Room.
2. User enters Width Along Street and Depth Into Parcel.
3. Area updates automatically.
4. User selects a street/side.
5. User activates map placement.
6. User clicks a valid street location.
7. System detects host parcel(s).
8. Electric Room is oriented according to the street.
9. Temporary map preview appears.
10. WPF schematic preview updates.
11. One-parcel placement works.
12. Two-parcel boundary placement works.
13. Host parcels are automatically clipped/carved.
14. Source data remains untouched.
15. Invalid placement is rejected with a useful message.
16. Changing dimensions updates the geometry.
17. Moving/reselecting the location updates the geometry.
18. Final Feature Class contains the Electric Room separately.
19. Existing parcel workflow remains functional.
20. Light/Dark ArcGIS Pro themes continue to work.
21. Build succeeds without errors.

==================================================
22. DEVELOPMENT RULE
====================

Before modifying anything:

1. Inspect the existing project.
2. Identify the current BlockConfiguration model.
3. Identify the geometry generation engine.
4. Identify the current Dynamic Preview.
5. Identify the existing Map Overlay implementation.
6. Identify how map interaction/tools are currently implemented.
7. Identify the existing parcel clipping/difference logic, if any.
8. Identify the existing validation framework.
9. Identify the final Feature Class generation pipeline.

Do NOT modify files during the initial analysis.

First provide:

- Existing architecture relevant to this feature.
- Files/classes that should be reused.
- Files/classes that need modification.
- New classes/services that are actually necessary.
- Proposed implementation flow.
- Potential ArcGIS Pro SDK/threading issues.
- Geometry edge cases.
- UI changes.

Then STOP and wait for approval before implementing.

After implementation:

- Build the project.
- Fix compilation errors.
- Verify existing functionality.
- Verify the Electric Room workflow.
- Report changed files and why.
- Report any remaining limitations.
