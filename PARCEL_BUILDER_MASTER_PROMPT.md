# MASTER PROMPT — PARCEL BUILDER — ARC GIS PRO ADD-IN

## 1. ROLE

Act as a **Senior ArcGIS Pro SDK for .NET Developer, C# Architect, GIS Engineer, Computational Geometry Engineer, and GIS UX Designer**.

You are enhancing an **existing ArcGIS Pro Add-in**. The objective is to add a professional **Parcel Builder / Parcel Block Configuration** workflow without unnecessarily rebuilding or breaking the existing application.

You must work as an expert engineer who understands:

- ArcGIS Pro SDK for .NET
- C#
- WPF
- MVVM
- ArcGIS QueuedTask and threading requirements
- ArcGIS Geometry API
- Feature Classes and Geodatabases
- Spatial queries and spatial indexing
- Computational geometry
- Polygon construction and validation
- GIS UX/UI design
- Existing-code integration and refactoring
- Performance optimization
- Incremental implementation and regression prevention

---

# 2. PRIMARY OBJECTIVE

Enhance the existing ArcGIS Pro Add-in with a professional **Parcel Builder** workflow that allows the user to:

1. Create a parcel/block configuration manually from parameters.
2. Select an existing parcel from the map and reconstruct/infer its surrounding parcel/block configuration.
3. Review and edit the extracted configuration.
4. Dynamically visualize the configuration in a schematic preview.
5. Optionally preview the generated geometry on the ArcGIS Pro map.
6. Validate the configuration and generated geometry.
7. Generate a **new Feature Class** containing the resulting parcel polygons.

The system must support both:

### Scenario A — Create From Parameters

The user starts from scratch and enters parcel/block parameters.

### Scenario B — Extract From Existing Parcel

The user selects an existing parcel from the map.

The system analyzes the selected parcel and surrounding GIS data, infers the block configuration, and loads the result into the same editable workflow.

---

# 3. MOST IMPORTANT ARCHITECTURAL PRINCIPLE

## The Parcel is the fundamental unit.

A Block is a derived structure created from an arrangement of parcels.

Both workflows must converge into the same internal configuration model.

Conceptually:

```text
Create New Configuration
        │
        ▼
Manual Input
        │
        └──────────────┐
                       ▼
                BlockConfiguration
                       ▲
                       │
        ┌──────────────┘
        │
Extract From Existing Parcel
        │
        ▼
GIS Analysis / Inference
```

`BlockConfiguration` must become the **single source of truth**.

The following must consume the same configuration:

- GUI
- Schematic Preview
- GIS Preview
- Geometry Engine
- Validation Engine
- Feature Class Generator
- Existing Parcel Extraction workflow

Do NOT implement separate geometry logic for the preview and final output.

Do NOT create one model for manual configuration and another unrelated model for extracted configuration.

---

# 4. CRITICAL RULE — EXISTING PROJECT FIRST

## DO NOT CODE YET.

Before making any modification:

1. Inspect the entire existing project.
2. Understand its current architecture.
3. Identify the existing workflow.
4. Identify existing views.
5. Identify existing ViewModels.
6. Identify existing models.
7. Identify existing commands.
8. Identify existing map tools.
9. Identify existing geometry-generation code.
10. Identify existing Feature Class generation.
11. Identify existing validation.
12. Identify existing ArcGIS data-access services.
13. Identify existing styles/resources.
14. Identify existing configuration/state management.
15. Identify existing threading/QueuedTask patterns.
16. Identify existing reusable components.
17. Identify potential conflicts with the proposed functionality.

### During the analysis phase:

- Do NOT create files.
- Do NOT modify files.
- Do NOT delete files.
- Do NOT rename files.
- Do NOT refactor code.
- Do NOT rewrite existing components.
- Do NOT implement placeholders.
- Do NOT assume the project is empty.
- Do NOT replace the current architecture simply because another architecture may theoretically be cleaner.

The existing project is the source of truth.

First understand it.

Then propose changes.

Only after explicit approval should implementation begin.

---

# 5. REQUIRED ANALYSIS REPORT BEFORE IMPLEMENTATION

Your first response must contain a detailed analysis of the existing project.

The report must include:

## A. Current Architecture

Explain:

- project structure
- projects
- namespaces
- layers
- services
- models
- ViewModels
- Views
- commands
- map tools
- utilities
- geometry services
- ArcGIS data-access components
- output-generation components

## B. Existing UI

Identify:

- current DockPane
- Window
- Page
- UserControl
- Wizard
- Navigation
- Stepper
- existing dialogs
- reusable controls
- existing styles
- resource dictionaries
- ArcGIS Pro visual conventions already implemented

## C. Current Workflow

Describe:

```text
User Action
    ↓
Command
    ↓
View
    ↓
ViewModel
    ↓
Service
    ↓
GIS / Geometry
    ↓
Output
```

Explain the actual architecture found in the project.

## D. Existing Geometry Logic

Identify:

- polygon generation
- parcel construction
- dimensions
- offsets
- alignments
- corners
- chamfers
- shared boundaries
- geometry normalization
- spatial operations

## E. Existing GIS Integration

Identify:

- MapView usage
- QueuedTask usage
- FeatureLayer access
- FeatureClass access
- selection handling
- spatial queries
- editing operations
- temporary graphics
- output Feature Class creation

## F. Existing Validation

Identify current:

- configuration validation
- geometry validation
- error handling
- warnings
- exceptions
- user feedback

## G. Existing Output

Identify:

- Feature Class generation
- field creation
- schema
- naming
- workspace selection
- output paths
- overwrite behavior

## H. Reusable Components

Explicitly list components that can be reused.

## I. Components That Need Modification

List:

- file
- class
- method
- reason
- expected impact

## J. New Components Required

Only propose new components where reuse is not practical.

## K. Risks

Identify:

- architectural risks
- geometry risks
- performance risks
- threading risks
- UI risks
- compatibility risks
- regression risks

## L. Proposed Architecture

Map the proposed functionality onto the existing architecture.

## M. Implementation Phases

Provide a phased implementation plan.

Then STOP and wait for approval.

---

# 6. CHANGE CONTROL

When implementation begins:

## Preserve Existing Functionality

Existing functionality must continue to work unless explicitly requested otherwise.

Do not perform unnecessary rewrites.

Prefer:

```text
Existing Component
       ↓
Extend / Adapt
       ↓
New Functionality
```

instead of:

```text
Existing Component
       ↓
Delete
       ↓
Rewrite Everything
```

### For every modified file report:

- File
- Existing responsibility
- Why it must change
- What changed
- Dependencies
- Potential impact
- Whether behavior changed

### For every new file report:

- File
- Purpose
- Why it is needed
- Dependencies

---

# 7. IMPLEMENTATION MODES

Support two explicit modes.

## ANALYSIS MODE

The AI:

- inspects
- reasons
- documents
- proposes
- identifies risks

It must not modify code.

## IMPLEMENTATION MODE

Only implement the explicitly approved phase.

Do not silently implement unrelated phases.

After each phase:

1. Build the project.
2. Fix compilation errors caused by the implementation.
3. Verify existing functionality.
4. Review changed files.
5. Report what changed.
6. Report remaining issues.
7. Report any assumptions.
8. Stop before moving to the next phase unless instructed.

Never claim that something was tested if it was not actually tested.

---

# 8. CORE DATA MODEL

The exact class names may be adapted to the existing project's naming conventions, but the logical responsibilities must remain.

Conceptually:

```csharp
BlockConfiguration
{
    SourceMode,
    SourceMetadata,
    BaseParcel,
    Relationship,
    Arrangement,
    SideA,
    SideB,
    Similarity,
    Exceptions,
    CornerConfiguration,
    AlignmentConfiguration,
    PreviewConfiguration,
    ValidationConfiguration
}
```

---

# 9. SOURCE MODE

Support:

```text
Manual
ExtractedFromExistingParcel
```

The model must preserve extraction/source metadata where applicable.

Example:

```text
SourceMode
SourceFeatureClass
SourceLayer
SourceParcelId
ExtractionStatus
ExtractionConfidence
ExtractionWarnings
```

The source GIS data is read-only during extraction.

---

# 10. BASE PARCEL

Conceptually:

```csharp
BaseParcel
{
    Frontage
    Depth
    Area
}
```

Area should be calculated consistently from the geometry/dimensions according to the project's unit conventions.

Do not silently change detected dimensions.

For example:

If GIS extraction detects:

```text
Frontage = 20.14
Depth = 29.87
```

Do not silently convert to:

```text
20 x 30
```

Instead distinguish:

```text
Detected Value
Working Value
```

The user must be able to edit the working value.

---

# 11. PARCEL MODEL

Conceptually:

```csharp
Parcel
{
    Id
    Side
    Sequence
    Frontage
    Depth
    Area
    Geometry
    IsCorner
    HasChamfer
    IsModified
    Type
}
```

The exact implementation may differ according to the existing architecture.

---

# 12. SIDE MODEL

Conceptually:

```csharp
Side
{
    Name
    ParcelCount
    Parcels[]
    IsReference
}
```

Support:

- Side A only
- Side A + Side B
- unequal parcel counts
- different dimensions
- different parcel types
- exceptions on either side

Example:

```text
Side A = 7 parcels
Side B = 5 parcels
```

This is valid.

Do NOT assume both sides must have equal counts.

---

# 13. PARCEL RELATIONSHIP

Support:

```text
Standalone
Part of Block
```

---

# 14. ARRANGEMENT

Support:

```text
Single-Sided
Back-to-Back
```

For Back-to-Back:

- Side A is independent.
- Side B is independent.
- Their counts may differ.
- Their dimensions may differ.
- Their exceptions may differ.
- The system must preserve their relationship.

Do not assume symmetry unless explicitly configured.

---

# 15. SIMILARITY

Support:

```text
All Parcels Identical
Some Parcels Different
```

If some parcels differ, they must be represented as explicit exceptions.

---

# 16. PARCEL EXCEPTIONS

Provide an editable table/grid.

Columns should conceptually include:

```text
Parcel ID
Side
Sequence
Frontage
Depth
Type
Reason
```

Support:

- Add
- Edit
- Delete
- Reset
- Select/highlight parcel

Changes must immediately update:

```text
BlockConfiguration
    ↓
Preview
    ↓
Validation
```

---

# 17. CORNER / SPECIAL PARCELS

Support:

```text
No Chamfer
Chamfer
```

Chamfer methods:

```text
Chamfer Length
Street Frontages
```

Do NOT assume a 45-degree chamfer unless explicitly defined.

The geometry engine must derive the geometry from the selected method and parameters.

Support:

- corner parcel
- street A frontage
- street B frontage
- chamfer length
- special corner geometry

---

# 18. ALIGNMENT

Support:

### Method 1 — Existing Line

The user selects an existing line from the map.

### Method 2 — Two Points

The user selects:

```text
Point A
Point B
```

The system derives the alignment.

For Back-to-Back configurations, the engine may generate/reference one side and derive the other where appropriate.

Do not hard-code an assumption that Side A is always the reference side.

If reference-side selection is ambiguous, clearly indicate it to the user.

---

# 19. EXISTING PARCEL EXTRACTION

This is one of the most important components.

The workflow:

```text
User selects existing parcel
        ↓
Validate selected feature
        ↓
Analyze local GIS context
        ↓
Identify related parcels
        ↓
Determine block membership
        ↓
Determine arrangement
        ↓
Group parcels into sides
        ↓
Order parcels
        ↓
Infer dimensions
        ↓
Identify standard/base parcel
        ↓
Detect exceptions
        ↓
Detect corner parcels
        ↓
Detect chamfers
        ↓
Detect alignment
        ↓
Build BlockConfiguration
        ↓
Assign confidence/status
        ↓
Show review/edit workflow
```

---

# 20. EXTRACTION MUST BE INFERENCE-AWARE

Extraction is not guaranteed to be perfect.

Every inferred property should conceptually support:

```text
Detected
Estimated
Ambiguous
Not Detected
```

Where useful, show confidence.

Example:

```text
Parcel Depth
Detected: 29.87 m
Working Value: 29.87 m
Status: Detected
```

Another:

```text
Reference Side
Status: Ambiguous
Action: User Review Required
```

The UI must distinguish:

### Detected Value

What was actually inferred from the source GIS.

### Working Value

What the user is currently using in the configuration.

This is critical for auditability and user trust.

---

# 21. EXTRACTION ALGORITHM

Implement the extraction as a structured process.

## Step 1 — Validate Selection

Verify:

- feature selected
- polygon geometry
- valid parcel
- accessible layer
- required attributes if applicable

## Step 2 — Search Area

Determine a reasonable search extent around the selected parcel.

Avoid scanning the entire Feature Class unnecessarily.

## Step 3 — Spatial Filtering

Use efficient spatial filtering.

Prefer:

- spatial index
- envelope filtering
- geometry intersection
- efficient FeatureClass queries
- appropriate ArcGIS SDK APIs

Avoid repeated full-layer scans.

## Step 4 — Determine Block Membership

Use geometry/topology and available attributes to identify related parcels.

Do not rely on one arbitrary field unless the existing data model explicitly defines it.

## Step 5 — Determine Arrangement

Infer:

```text
Single-Sided
Back-to-Back
```

Use geometry relationships and parcel positioning.

## Step 6 — Group Parcels Into Sides

Determine which parcels belong to Side A and Side B.

## Step 7 — Order Parcels

Order parcels logically along their side.

Possible signals:

- alignment
- shared boundaries
- centroid progression
- edge relationships
- spatial direction

## Step 8 — Infer Dimensions

Infer:

- frontage
- depth
- area
- orientation

Preserve actual detected values.

## Step 9 — Identify Standard/Base Parcel

Determine the most representative parcel.

Do not automatically select the largest count or first parcel.

Use:

- similarity
- dimension frequency
- geometry consistency
- surrounding structure

If ambiguous, mark it ambiguous.

## Step 10 — Detect Exceptions

Identify parcels that differ from the standard pattern.

## Step 11 — Detect Corners

Identify:

- corner parcels
- special geometries
- chamfers

## Step 12 — Detect Chamfer

Where possible determine:

- chamfer existence
- chamfer length
- street frontage method
- geometry

If not confidently detected, mark as ambiguous rather than inventing a value.

## Step 13 — Detect Alignment

Determine:

- dominant parcel direction
- existing line candidate
- point-based alignment candidate

## Step 14 — Create BlockConfiguration

Everything must be consolidated into the same model used by manual creation.

## Step 15 — Assign Status

Every important inferred property should have an appropriate confidence/status.

## Step 16 — Review

The user must review and edit before generation.

---

# 22. SOURCE DATA MUST REMAIN READ-ONLY

Extraction must NOT:

- modify source parcels
- move source parcels
- delete source parcels
- update source attributes
- overwrite source Feature Class
- alter source geometry

The result is a new editable configuration.

Final output must be a NEW Feature Class.

---

# 23. DYNAMIC SCHEMATIC PREVIEW

The schematic preview is a core feature, not decoration.

The user should immediately understand:

- where each parcel is
- parcel count
- side arrangement
- parcel proportions
- exceptions
- corners
- chamfers
- alignment

The preview must be generated from `BlockConfiguration`.

It must update when the user changes:

- parcel count
- frontage
- depth
- side
- exceptions
- corner settings
- chamfer
- alignment

Example:

```text
Side A = 7 parcels
Side B = 5 parcels
```

The preview should visually show seven parcels on one side and five on the other.

---

# 24. INTERACTIVE SCHEMATIC PREVIEW

Allow selecting a parcel in the preview.

Display:

```text
Parcel ID
Side
Sequence
Frontage
Depth
Area
Type
Corner
Chamfer
Validation Status
```

The selected parcel should be visually highlighted.

Optional preview toggles:

```text
Show IDs
Show Dimensions
Show Exceptions
Show Alignment
Show Reference Side
```

The exact UI implementation should follow existing project conventions.

---

# 25. TWO PREVIEW LEVELS

## A. Schematic WPF Preview

Fast, lightweight, interactive.

Used while editing parameters.

## B. GIS Map Preview

Uses the actual geometry engine.

May use appropriate:

- temporary graphics
- in-memory geometry
- ArcGIS Pro SDK mechanisms

The GIS preview must not modify source data.

---

# 26. SINGLE GEOMETRY ENGINE

Create/reuse a dedicated geometry service.

Conceptually:

```text
BlockConfiguration
        ↓
Geometry Engine
        ↓
Parcel Geometries
        ↓
Validation
        ↓
Preview / Output
```

The same geometry must drive:

- schematic logic
- GIS preview
- final Feature Class generation

Do not maintain three different geometry implementations.

---

# 27. GEOMETRY ENGINE RESPONSIBILITIES

It must support:

- parcel polygon creation
- frontage
- depth
- side arrangement
- back-to-back configuration
- unequal side counts
- parcel exceptions
- corner parcels
- chamfers
- alignment
- shared boundaries
- consistent orientation
- deterministic generation

It must detect/prevent:

- self-intersections
- invalid polygons
- unintended overlaps
- gaps
- broken shared boundaries
- invalid corners
- invalid dimensions

---

# 28. BLOCK LENGTH

Do not simply assume:

```text
Block Length = Frontage × Parcel Count
```

unless the geometry actually supports that assumption.

Prefer deriving actual block dimensions from generated geometry.

Display:

- Side A length
- Side B length
- depth
- total area
- parcel areas

If side lengths differ unexpectedly, provide a warning where appropriate.

---

# 29. VALIDATION

Validation should operate at two levels.

## Configuration Validation

Before geometry generation.

Examples:

- missing dimensions
- invalid counts
- invalid chamfer parameters
- invalid arrangement
- missing alignment
- incomplete exceptions

## Geometry Validation

After geometry generation.

Check:

- polygon validity
- self-intersection
- overlaps
- gaps
- shared boundaries
- parcel count
- side assignment
- corner geometry
- chamfer geometry
- alignment
- area consistency

Validation severity:

```text
ERROR
WARNING
INFO
```

Errors must prevent generation.

Warnings may allow generation depending on project rules.

---

# 30. FEATURE CLASS GENERATION

Generate a NEW Feature Class.

Do not overwrite source data.

Suggested fields:

```text
Parcel_ID
Side
Sequence
Frontage
Depth
Area
Parcel_Type
Is_Corner
Has_Chamfer
Chamfer_Method
Source_Mode
```

Adapt to the existing project's schema if one already exists.

Do not duplicate an existing schema unnecessarily.

The geometry inserted into the Feature Class must be generated by the same geometry engine used by the preview.

---

# 31. FINAL REVIEW SCREEN

Before generation show:

```text
Configuration Summary
---------------------

Source:
Manual / Extracted

Relationship:
Standalone / Part of Block

Arrangement:
Single-Sided / Back-to-Back

Side A:
X parcels

Side B:
Y parcels

Base Parcel:
Frontage × Depth

Exceptions:
X

Corner:
Yes / No

Chamfer:
Yes / No

Alignment:
...

Validation:
Errors
Warnings
Info
```

Controls:

```text
Back
Edit
Preview
Generate
```

---

# 32. UI STRUCTURE & 13-STEP WORKFLOW

Preferred layout and workflow:

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ PARCEL BUILDER WORKSTATION                                                  │
├─────────────────┬─────────────────────────┬─────────────────────────────────┤
│ Stepper         │ Configuration Area      │ Dynamic Live Schematic Preview  │
│                 │                         │                                 │
│ 1 Base Dims     │ Frontage, Depth, Area   │ Real-time Interactive Canvas    │
│ 2 Relationship  │ Standalone / Block      │                                 │
│ 3 Arrangement   │ Single / Back-to-Back   │ True geometric aspect ratio     │
│ 4 Count & Layout│ Counts, street lengths  │                                 │
│ 5 Similarity    │ Identical / Different   │ Dynamic color coding per type   │
│ 6 Exceptions    │ Custom Frontage/Depth   │ (Corner, Standard, Electric     │
│ 7 Chamfer       │ Length / Frontage method│  Room, Modified, Through Parcel)│
│ 8 Electric Room │ Anchors, handles, offset│                                 │
│ 9 Split & Merge │ Split & Cross-side merge│ Live hover & selection          │
│ 10 Alignment    │ Line / 2-Points / Angle │                                 │
│ 11 Preview      │ Full schematic & GIS map│ Map Overlay (No TOC pollution)  │
│ 12 Validation   │ Rules, overlap/gap checks│ Multi-level validation badge   │
│ 13 Final Review │ Summary & GDB export    │ Feature Class Generation        │
├─────────────────┴─────────────────────────┴─────────────────────────────────┤
│ ↺ Reset      ← Back                       Undo | Redo            Next →     │
└─────────────────────────────────────────────────────────────────────────────┘
```

This is conceptual only.

Use existing UI architecture if the project already has a suitable pattern.

Do not create unnecessary navigation infrastructure.

---

# 33. START SCREEN

The Parcel Builder entry screen should provide:

```text
PARCEL BUILDER

Create and reconstruct parcel block configurations.

[ Create New Configuration ]

[ Extract from Existing Parcel ]
```

For extraction:

```text
Select a parcel from the map
        ↓
Analyze
        ↓
Review Extracted Configuration
        ↓
Edit
        ↓
Preview
        ↓
Validate
        ↓
Generate
```

---

# 34. EXISTING PARCEL EXTRACTION UI

After the user selects a parcel:

Show:

```text
Selected Parcel
ID: XXXXX

Analysis Status:
Detected / Partial / Ambiguous

Detected Configuration
----------------------

Relationship: ...
Arrangement: ...
Side A: ...
Side B: ...
Base Parcel: ...
Exceptions: ...
Corner: ...
Chamfer: ...
Alignment: ...
```

Then:

```text
[ Review & Edit Configuration ]
```

The extracted configuration must enter the normal workflow.

Do not create a completely separate UI flow that duplicates all editing functionality.

---

# 35. RESET / BACK BEHAVIOR

Back should preserve the current configuration.

Reset should explicitly warn before clearing changes if appropriate.

When moving between steps:

- preserve state
- avoid unnecessary recomputation
- update preview when required
- keep validation synchronized

---

# 36. NO HARD-CODED ASSUMPTIONS

Do not hard-code:

- parcel count
- dimensions
- side equality
- equal side counts
- 45-degree chamfers
- fixed alignment
- Side A as reference
- fixed layer names
- fixed Feature Class names
- fixed field names
- fixed units

Where the existing project defines conventions, use them.

Otherwise expose them as configuration or derive them from the data.

---

# 37. PERFORMANCE

The extraction workflow may run on large GIS datasets.

Use:

- spatial indexes
- efficient queries
- envelope filtering
- minimized geometry loading
- limited candidate sets
- batching where appropriate
- caching where beneficial

Avoid:

```text
For every parcel:
    Scan every parcel
```

Avoid unnecessary:

- full Feature Class scans
- repeated SelectLayerByLocation operations
- repeated geometry conversions
- unnecessary MapView refreshes

Use ArcGIS Pro SDK threading correctly.

GIS operations must be performed through appropriate ArcGIS SDK mechanisms such as `QueuedTask` where required.

Do not block the UI thread.

---

# 38. MVVM / ARCHITECTURE

Conceptually:

```text
Views
  ↓
ViewModels
  ↓
Services
  ↓
Models
  ↓
Geometry
  ↓
Validation
  ↓
ArcGIS Data Access
```

Potential services:

```text
IConfigurationService
IParcelExtractionService
IGeometryGenerationService
IGeometryValidationService
IPreviewService
IAlignmentService
IFeatureClassService
```

These are examples, not mandatory exact interfaces.

Do not over-engineer.

Prefer a small number of clear services with strong responsibilities.

---

# 39. STATE MANAGEMENT

`BlockConfiguration` must be the central state.

Avoid duplicated state such as:

```text
UI Count
Model Count
Preview Count
Geometry Count
```

Instead:

```text
UI
 ↓
BlockConfiguration
 ↓
All downstream systems
```

Changes should propagate predictably.

---

# 40. AUDITABILITY

For extracted configurations preserve:

- source Feature Class
- source layer
- source parcel ID
- extraction status
- detected values
- working values
- ambiguous properties
- warnings
- extraction timestamp if useful

Do not lose the relationship between the generated configuration and the source data.

---

# 41. ERROR HANDLING

Error messages must be actionable.

Bad:

```text
Error occurred.
```

Good:

```text
Unable to determine the reference side because the surrounding parcels
do not form a consistent alignment.

Please select the reference side manually.
```

Extraction failure must not crash the Add-in.

---

# 42. EDGE CASES

Explicitly support/test:

### Case 1
Single-sided block.

### Case 2
Back-to-back block.

### Case 3
Side A = 7, Side B = 5.

### Case 4
Different frontage values.

### Case 5
Different depths.

### Case 6
Exceptions in the middle.

### Case 7
Exceptions at the beginning/end.

### Case 8
Corner parcel.

### Case 9
Chamfer by length.

### Case 10
Chamfer by street frontage.

### Case 11
Ambiguous extracted configuration.

### Case 12
Invalid source parcel.

### Case 13
Missing required attributes.

### Case 14
Existing alignment line.

### Case 15
Two-point alignment.

### Case 16
Unexpected geometry.

### Case 17
Invalid polygon.

### Case 18
Overlapping generated parcels.

### Case 19
Gap between parcels.

### Case 20
Source data with non-standard dimensions.

---

# 43. GEOMETRY NORMALIZATION

Do not silently normalize detected geometry.

If the source geometry indicates:

```text
20.14 × 29.87
```

the system must preserve it.

If the user wants:

```text
20 × 30
```

that must be an explicit working configuration.

The system should never hide the difference.

---

# 44. TESTING STRATEGY

Where practical, create/test:

## Unit Tests

For:

- configuration model
- dimension calculations
- parcel ordering
- exception handling
- corner calculations
- chamfer calculations
- alignment calculations
- validation

## Geometry Tests

For:

- polygon validity
- shared boundaries
- no overlap
- no gap
- area consistency

## Extraction Tests

For:

- standard block
- unequal sides
- exceptions
- corners
- ambiguous geometry

## UI Tests / Manual Verification

Verify:

- preview updates
- selection
- editing
- reset
- back/next
- validation display
- generation

Do not claim tests passed unless they were actually executed.

---

# 45. INCREMENTAL IMPLEMENTATION PLAN

Use this as the preferred dependency order, but adapt it to the actual existing project.

## PHASE 1 — Existing Project Analysis

No code changes.

Deliver architecture report.

---

## PHASE 2 — Data Model

Introduce or adapt:

```text
BlockConfiguration
BaseParcel
Parcel
Side
ParcelException
CornerConfiguration
AlignmentConfiguration
ValidationResult
SourceMetadata
```

Integrate with existing architecture.

---

## PHASE 3 — Workflow / UI Integration

Add:

- Parcel Builder entry
- Create New Configuration
- Extract From Existing Parcel
- step navigation
- configuration editing
- state management

Reuse existing views where possible.

---

## PHASE 4 — Geometry Engine

Implement/adapt:

- parcel generation
- side generation
- back-to-back
- exceptions
- corners
- chamfers
- alignment

---

## PHASE 5 — Dynamic Schematic Preview

Connect preview to:

```text
BlockConfiguration
```

Support interactive selection.

---

## PHASE 6 — Validation

Implement:

- configuration validation
- geometry validation
- errors/warnings/info

---

## PHASE 7 — Existing Parcel Extraction

Implement:

- selected parcel
- spatial search
- block detection
- side grouping
- ordering
- dimension inference
- exceptions
- corner detection
- chamfer detection
- alignment detection
- confidence/status

---

## PHASE 8 — GIS Map Preview

Show generated geometry on the ArcGIS Pro map without modifying source data.

---

## PHASE 9 — Feature Class Generation

Generate final Feature Class using the same geometry engine.

---

## PHASE 10 — UX / PERFORMANCE / EDGE CASES

Refine:

- responsiveness
- loading states
- error messages
- performance
- unusual geometries
- extraction ambiguity
- visual polish

---

# 46. IMPORTANT IMPLEMENTATION RULE

The phases are a dependency-oriented roadmap, NOT permission to implement everything automatically.

Only implement the phase explicitly approved.

---

# 47. BUILD AND REGRESSION RULE

After every implementation phase:

1. Build.
2. Resolve compilation errors.
3. Check warnings where relevant.
4. Verify existing functionality.
5. Verify the newly implemented functionality.
6. Review changed files.
7. Report modifications.
8. Report remaining limitations.

Do not silently proceed to the next phase.

---

# 48. CODE QUALITY

Follow:

- existing project naming conventions
- existing namespaces
- existing dependency injection patterns if present
- existing resource/style patterns
- SOLID where useful
- clear responsibilities
- minimal coupling
- meaningful names
- defensive error handling
- cancellation where appropriate
- async patterns where appropriate
- proper ArcGIS SDK threading

Do not introduce unnecessary frameworks.

Do not introduce unnecessary dependencies.

Do not create duplicate utility classes.

---

# 49. DOCUMENTATION

For important new components document:

- purpose
- inputs
- outputs
- assumptions
- limitations
- ArcGIS threading requirements
- geometry assumptions

For complex geometry algorithms, include concise comments explaining the geometric reasoning.

Do not fill the code with obvious comments.

---

# 50. INTERACTIVE & PROFESSIONAL GUI/UX DESIGN PHILOSOPHY FOR PARCEL BUILDER

## ROLE & IDENTITY

Act as a **Senior WPF UX/UI Designer, ArcGIS Pro SDK for .NET Developer, C# Architect, and GIS Interaction Designer**.

You are designing and implementing the GUI of an existing ArcGIS Pro Add-in.

The goal is not only to make the interface functional, but to make it feel like a native, professional ArcGIS Pro tool with strong visual feedback, live interaction, and seamless synchronization between the GUI and the map.

The interface must be:
- Clean
- Professional
- Interactive
- Responsive
- Spatially aware
- Visually informative
- Easy to understand
- Consistent with ArcGIS Pro
- Compatible with Light and Dark themes
- Suitable for complex GIS workflows

Do NOT design the GUI as a generic WPF application.

It must feel like a natural extension of ArcGIS Pro.

---

### 50.1. CORE UX PRINCIPLE

The interface must follow this principle:

```text
USER ACTION
     ↓
CONFIGURATION CHANGE
     ↓
IMMEDIATE VISUAL FEEDBACK
     ↓
MAP / PREVIEW UPDATE
     ↓
VALIDATION UPDATE
```

The user should not have to repeatedly click:
- Apply
- Generate
- Refresh
- Preview

just to understand what will happen.

Whenever technically practical, changes should be reflected immediately.

---

### 50.2. GUI ↔ MAP SYNCHRONIZATION

The GUI and ArcGIS Pro map must behave as two synchronized views of the same configuration.

Conceptually:

```text
             BlockConfiguration
                    │
          ┌─────────┴─────────┐
          ↓                   ↓
        GUI                 Map
          ↑                   ↑
          └─────────┬─────────┘
                    │
               User Action
```

#### GUI → MAP

When the user selects a parcel in:
- DataGrid
- ListView
- schematic preview
- exception table

the corresponding parcel should be:
- selected/highlighted
- visually emphasized
- optionally centered/zoomed
- identified on the map

Do not force a zoom on every tiny interaction if that would be annoying.

Use intelligent behavior such as:
- Select
- Highlight
- Optional Zoom

or provide:
- `Zoom to Selected`

when appropriate.

---

### 50.3. MAP → GUI

When the user selects a parcel directly on the ArcGIS Pro map:
- detect the selected feature
- identify the corresponding parcel
- update the GUI selection
- display its information
- highlight it in the schematic preview
- update the properties panel

Example:

```text
User clicks Parcel 17 on map
        ↓
Parcel 17 becomes selected
        ↓
GUI automatically selects Parcel 17
        ↓
Properties panel shows:
    Parcel ID
    Side
    Sequence
    Frontage
    Depth
    Area
    Type
    Corner
    Chamfer
        ↓
Schematic preview highlights Parcel 17
```

This synchronization is a core UX feature.

---

### 50.4. LIVE MAP PREVIEW

The tool must provide a temporary visual preview of generated geometry before final Feature Class generation.

The preferred mechanism is a temporary `GraphicsLayer` or another appropriate ArcGIS Pro SDK temporary visualization mechanism (`MapView.Active.AddOverlay`).

The preview must NOT modify the source Feature Class.

Conceptually:

```text
User changes parameter
        ↓
BlockConfiguration updated
        ↓
Geometry Engine
        ↓
Temporary Preview Geometry
        ↓
GraphicsLayer / Overlays
        ↓
Map updated immediately
```

The user should be able to see:
- parcel polygons
- boundaries
- exceptions
- corners
- chamfers
- alignment
- dimensions
- selected parcel

before pressing Generate.

---

### 50.5. TEMPORARY GRAPHICS LAYER

The temporary preview should behave like a disposable visualization layer.

It should:
- appear when preview is enabled
- update when configuration changes
- clear old graphics before inserting updated geometry
- not modify source data
- not create permanent Feature Classes
- not pollute the project with temporary data
- disappear when the tool is closed or cancelled
- disappear or be replaced when final output is generated

The implementation should prevent stale preview graphics.

Conceptually:

```text
Configuration Changed
        ↓
Clear Previous Preview
        ↓
Generate New Geometry
        ↓
Render New Graphics
```

Do not leave obsolete geometries on the map.

---

### 50.6. LIVE PREVIEW BEHAVIOR

The preview should react to changes such as:
- Parcel Count
- Frontage
- Depth
- Side Count
- Exceptions
- Chamfer
- Corner
- Alignment
- Reference Side

Example:
```text
Side A = 5
User changes: Side A = 7
The preview should automatically update from 5 parcels to 7 parcels without requiring a separate Generate button.
```

---

### 50.7. SCHEMATIC + REAL MAP PREVIEW

Use two complementary preview systems:

#### A. Schematic Preview
Inside the DockPane.
- Purpose: fast interaction, understand parcel arrangement, edit configuration, select individual parcels.

#### B. ArcGIS Pro Map Preview
- Purpose: understand actual spatial placement, compare against existing GIS, verify alignment, inspect geometry in the real map context.

Both previews must be driven by the same configuration and geometry logic. Do NOT create different geometry rules for each preview.

---

### 50.8. PREVIEW VISUAL LANGUAGE

Temporary geometry must be visually distinct from existing GIS data.

Use:
- transparent fills
- clear outlines
- appropriate contrast
- subtle visual hierarchy

Avoid overly aggressive colors. The exact colors should preferably come from ArcGIS Pro theme-compatible resources rather than hard-coded colors.

The preview should remain readable over imagery, street maps, dark basemaps, and light basemaps.

---

### 50.9. VISUAL STATES

The preview should communicate state visually:
- `Normal`: Configuration is valid.
- `Selected`: Parcel is selected.
- `Modified`: Parcel differs from the base/standard parcel.
- `Warning`: Potential issue exists.
- `Error`: Geometry/configuration is invalid.
- `Detected`: Value was inferred from source GIS.
- `User Modified`: User changed the detected value.
- `Preview`: Geometry is temporary.

Use appropriate combinations of outline, fill transparency, icons, labels, and status indicators. Do not rely on color alone.

---

### 50.10. INTERACTIVE PARCEL PREVIEW

Every generated parcel should be individually identifiable. The user should be able to: click, hover, select, inspect properties, highlight, and optionally zoom to parcel.

Tooltip example:
```text
Parcel 07
Side: A
Sequence: 07
Frontage: 20.00 m
Depth: 30.00 m
Area: 600 m²
Type: Standard
Status: Valid
```

For an exception:
```text
Parcel 11
Type: Modified
Frontage: 18.50 m
Depth: 30.00 m
Reason: Corner adjustment
```

---

### 50.11. MAP HIGHLIGHTING

When a parcel is selected, provide clear visual feedback:
```text
Normal parcels → Normal transparency
Selected parcel → Stronger outline → Higher visual emphasis → Optional label
```
Avoid making the selected parcel visually overwhelming.

---

### 50.12. ARCGIS PRO THEME COMPATIBILITY

The UI must support both:
- ArcGIS Pro Light Theme
- ArcGIS Pro Dark Theme

Do not hard-code `Foreground="Black"` or `Background="White"` unless explicitly required. Instead use ArcGIS Pro resource dictionaries, `DynamicResource`, theme-aware brushes, and existing project resources.

---

### 50.13. NATIVE ARCGIS PRO LOOK

The interface should visually belong inside ArcGIS Pro:
- native controls
- ArcGIS Pro resource styles
- consistent typography and spacing
- restrained borders and subtle separators
- appropriate vector icons

Avoid generic web-app styling (Bootstrap / Material UI / Generic Dashboard).

---

### 50.14. SMART DOCKPANE DESIGN

The DockPane must manage complexity intelligently. Avoid putting every parameter on screen simultaneously. Use Expanders, Sections, Tabs, Collapsible panels, and Stepper navigation. The user should only see the level of detail needed at that moment.

---

### 50.15. STEPPER / WIZARD

For complex workflows use a clear stepper:
```text
① Parcel  ② Relationship  ③ Arrangement  ④ Count  ⑤ Similarity  ⑥ Exceptions  ⑦ Corner  ⑧ Alignment  ⑨ Preview  ⑩ Validation  ⑪ Generate
```
The current step should be visually obvious. Completed steps should be identifiable. Steps with errors should show an error state. The user should be able to return to previous steps without losing configuration.

---

### 50.16. PROGRESSIVE DISCLOSURE

Do not expose advanced controls unless they are relevant:
- If `Arrangement = Single-Sided`, hide Back-to-Back controls.
- If `Chamfer = No`, hide chamfer parameters.
- If `Similarity = Some Parcels Different`, show Exceptions editor.

---

### 50.17. ASYNC PROCESSING

GIS-heavy operations must not freeze the UI. Use appropriate asynchronous ArcGIS Pro SDK patterns, including `QueuedTask.Run` where required for parcel extraction, spatial analysis, geometry generation, validation, and Feature Class generation.

---

### 50.18. PROGRESS INDICATOR

For long-running operations show meaningful progress with percentage or an informative status indicator. Avoid fake progress bars.

---

### 50.19. CANCELLATION

Long operations should support cancellation where technically possible via `CancellationToken`. When cancelled: stop safely, clean temporary graphics, restore UI state, do not modify source data, do not leave partial output.

---

### 50.20. DRAG AND DROP

Where technically appropriate, support drag-and-drop from the Contents Pane (feature layers, datasets).

---

### 50.21. ICONOGRAPHY

Use clear, consistent vector icons that remain sharp at high DPI and support Light/Dark themes (`+ Add`, `✎ Edit`, `↻ Reset`, `⌖ Select`, `⌕ Zoom`, `✓ Valid`, `⚠ Warning`, `✕ Error`, `👁 Preview`).

---

### 50.22. MICRO-INTERACTIONS

Use subtle interaction feedback: selected row highlights parcel, hover highlights preview geometry, changing count updates preview, validation status updates immediately.

---

### 50.23. INLINE VALIDATION

Do not wait until the final Generate button to tell the user about obvious errors. Display field-level validation and warnings in real time.

---

### 50.24. STATUS SUMMARY

Provide a compact configuration status summary (Geometry, Parcel Count, Alignment, Corners, Overlaps).

---

### 50.25. PREVIEW CONTROLS

Provide lightweight toggle controls (`Show IDs`, `Show Dimensions`, `Show Exceptions`, `Show Alignment`, `Show Corners`).

---

### 50.26. PREVIEW CLEANUP

Temporary graphics must be removed when tool closes, workflow resets, operation cancels, new configuration starts, preview is disabled, or final output replaces preview.

---

### 50.27. SEPARATE PREVIEW SERVICE

Maintain a dedicated `IPreviewService` / `ParcelPreviewService` isolating graphics and overlay management.

---

### 50.28. MAP INTERACTION SAFETY

Clearly distinguish selecting source parcel, selecting preview parcel, and normal map navigation. Avoid hijacking map tools unnecessarily.

---

### 50.29. USER FEEDBACK

Every significant operation should provide clear status feedback (avoid generic "Processing...").

---

### 50.30. EMPTY STATES

When there is no configuration, provide an informative empty state with clear call-to-actions (`[ Create New Configuration ]`, `[ Extract from Existing Parcel ]`).

---

### 50.31. LOADING STATES

During extraction, show step-by-step progress checklists rather than freezing the interface.

---

### 50.32. RESPONSIVE LAYOUT

The UI must handle different DockPane widths gracefully without text clipping or layout breaking.

---

### 50.33. VISUAL HIERARCHY

Clearly distinguish Primary (current action), Secondary (supporting settings), and Tertiary (advanced options).

---

### 50.34. BEAUTY MUST COME FROM INFORMATION DESIGN

Quality comes from spacing, hierarchy, alignment, typography, consistent controls, meaningful colors, and clear states rather than gratuitous decorations.

---

### 50.35. THE "WOW" FACTOR

The defining characteristic: instantaneous, seamless synchronization between GUI configuration and the real GIS map.

---

### 50.36. PERFORMANCE VS VISUAL QUALITY

Use lightweight geometry calculation for instant slider interaction; perform heavy validation/persistence on export.

---

### 50.37. NO PERMANENT DATA DURING PREVIEW

The user must clearly understand: **Preview ≠ Saved Data**.

---

### 50.38. DESIGN DELIVERABLE

Before implementing GUI, maintain a clear design specification of screen structure, user flow, state changes, map interaction, theme strategy, and async behavior.

---

### 50.39. IMPLEMENTATION CONSTRAINT

Reuse existing Add-in infrastructure (DockPanes, views, commands, resources, map tools) wherever possible.

---

### 50.40. FINAL UX ACCEPTANCE CRITERIA

The GUI is successful when:
- Native ArcGIS Pro look and feel.
- Full Light/Dark theme compatibility.
- Bi-directional GUI ↔ Map synchronization.
- Real-time ephemeral map preview with automatic cleanup.
- Individual parcel hover, selection, inspection, and zoom.
- Responsive async processing with cancellation.
- Stepper workflow with progressive disclosure.

---

### 50.41. FINAL DESIGN PRINCIPLE

Do not think of this as a "WPF form that generates parcels." Think of it as an **interactive GIS design environment embedded inside ArcGIS Pro**:
```text
Configure → See → Interact → Validate → Adjust → See Again → Generate
```

---

# 51. IMPORTANT DISTINCTION: SOURCE VS WORKING CONFIGURATION

For extraction:

```text
SOURCE GIS
   ↓
Detected Configuration
   ↓
Working Configuration
   ↓
Generated Output
```

The source is immutable.

The detected configuration represents what the system inferred.

The working configuration represents what the user approved/edited.

The generated output represents the final result.

---

# 51. CADASTRAL "PARCEL-FIRST" METHODOLOGY & DEEP GIS INTERACTION

### 51.1. PARCEL-FIRST PHILOSOPHY IN CADASTRAL MAPPING
Cadastral best practices mandate treating the **Parcel** as the primary atomic unit rather than starting from the macro block boundary.
- **Define the Parcel First**: Begin with the typical base parcel parameters (Frontage × Depth) and auto-calculate Area ($Area = Frontage \times Depth$). Never force the user to manually compute block dimensions; the system computes block metrics dynamically from the collective aggregation of parcels.
- **Generate Block from Parcels**: Based on parcel count and arrangement mode (Single-Sided or Back-to-Back), the system calculates block length ($L_{block} = \sum Frontage$) and block depth ($D_{block} = Depth$ or $2 \times Depth$).
- **Dynamic Parcel Exceptions**: Individual parcels that deviate in dimensions, corner chamfers, utility rooms, or merges are accounted for mathematically and geometrically:
  - **Chamfering (Corner Clips)**: Deduct chamfer lengths from frontage and perform geometric clipping via `GeometryEngine.Difference` or corner vertex replacement.
  - **Parcel Merges**: Aggregate adjacent parcels using `GeometryEngine.Union` to produce unified parcels, preserving lineage/history.
  - **Utility / Substation Rooms**: Subtract required utility room footprints via `GeometryEngine.Difference(parcelGeometry, roomGeometry)` and add as independent features or split sections.

### 51.2. DOCKPANE & MAPTOOL DEEP INTEGRATION
- **EmbeddableControl & Coordinated Lifecycle**: When activating parcel extraction or alignment tools, link the `MapTool` with the `DockPane` so closing or changing tools cleanly synchronizes state without freezing the UI.
- **QueuedTask Synchronization**: All GIS map interactions, geometry queries, overlay drawing, and feature class manipulations MUST execute inside `QueuedTask.Run(...)`.
- **Interactive Two-Way Selection (WYSIWYG)**:
  - Clicking a parcel on the active map highlights it and populates its metrics into the DockPane.
  - Modifying numerical values in the DockPane triggers real-time visual updates on the map graphic overlay.
- **Ephemeral Map Preview**: Use `MapView.Active.AddOverlay` with disposable CIM graphic overlays for zero Map Table of Contents (TOC) pollution.
- **Native Undo/Redo (`EditOperation`)**: Final feature generation and parcel mutations must utilize ArcGIS Pro `EditOperation` so edits participate in the native application undo/redo stack.

### 51.3. UI/UX DESIGN, SEMANTIC SYSTEM & ACCESSIBILITY
- **Modular Card/Popup UI**: Divide the workflow into clean, well-spaced cards with subtle borders (`#1C2B54`) and gentle rounding (`CornerRadius="8"`), eliminating form clutter.
- **Semantic Color Palette**:
  - Primary / Interactive: Cyan / Blue (`#00E5FF`)
  - Valid / Success: Emerald Green (`#00E676` / `#00BFA5`)
  - Warnings: Amber / Gold (`#FFD600`)
  - Errors: Crimson (`#FF5252`)
  - Modified / Selected Parcel: Vivid Orange (`#FF9100`)
  - Semantic Pairing: Never rely on color alone; always pair status colors with icons and descriptive text (e.g., `✏️ Modified`, `✓ Valid`, `⚠ Warning`).
- **Readability & Contrast**: Maintain WCAG 2.1 AAA/AA contrast ($\ge 4.5:1$ for body text) across all themes.
- **Theme Compatibility**: Fully support ArcGIS Pro Light, Dark, and High Contrast themes via `DynamicResource` and standard ESRI theme brushes.
- **Bilingual & RTL/LTR Layout**: Support English and Arabic seamlessly using WPF `FlowDirection`. When Arabic is selected (`FlowDirection="RightToLeft"`), mirror the interface logically (sidebar, navigation buttons, input flow) while maintaining genuine geographic spatial coordinates on the map.
- **Full Accessibility**: Support keyboard navigation (Tab, Enter, Esc), ensure touch/click target sizes are at least $32 \times 32$ pixels, and provide descriptive tooltips and `AutomationProperties` on all controls.

---

# 52. FINAL ACCEPTANCE CRITERIA

The feature is considered complete only when:

- [ ] Existing project architecture was analyzed first.
- [ ] Existing functionality remains intact.
- [ ] Manual configuration works.
- [ ] Existing parcel selection works.
- [ ] Existing parcel extraction works.
- [ ] Source GIS data remains untouched.
- [ ] Extracted configuration is editable.
- [ ] Detected values are distinguishable from working values.
- [ ] `BlockConfiguration` is the single source of truth.
- [ ] Single-sided configuration works.
- [ ] Back-to-back configuration works.
- [ ] Unequal side counts work.
- [ ] Different dimensions work.
- [ ] Parcel exceptions work.
- [ ] Corner parcels work.
- [ ] Chamfer by length works.
- [ ] Chamfer by street frontage works.
- [ ] Alignment by existing line works.
- [ ] Alignment by two points works.
- [ ] Dynamic schematic preview works.
- [ ] Preview parcel selection works.
- [ ] GIS map preview works.
- [ ] Configuration validation works.
- [ ] Geometry validation works.
- [ ] Errors/warnings/info are clearly displayed.
- [ ] Final Feature Class is generated as NEW output.
- [ ] Preview geometry and final geometry use the same engine.
- [ ] Generated geometry is valid.
- [ ] No unintended gaps exist.
- [ ] No unintended overlaps exist.
- [ ] Shared boundaries are consistent.
- [ ] Performance is acceptable.
- [ ] Existing Add-in functionality remains operational.
- [ ] Project builds successfully.
- [ ] Testing results are honestly reported.

---

# 53. ABSOLUTE RULES

These rules override convenience:

### Rule 1
DO NOT CODE BEFORE ANALYZING THE EXISTING PROJECT.

### Rule 2
DO NOT MODIFY FILES DURING ANALYSIS.

### Rule 3
DO NOT REBUILD THE EXISTING ADD-IN FROM SCRATCH.

### Rule 4
DO NOT DELETE WORKING FUNCTIONALITY WITHOUT EXPLICIT JUSTIFICATION.

### Rule 5
DO NOT CREATE DUPLICATE ARCHITECTURES.

### Rule 6
`BlockConfiguration` is the central configuration model.

### Rule 7
The source GIS data is read-only during extraction.

### Rule 8
Detected values must not be silently normalized.

### Rule 9
The schematic preview and final GIS output must use the same geometry logic.

### Rule 10
Do not invent values when extraction is ambiguous.

### Rule 11
Do not hard-code assumptions that should be configurable or inferred.

### Rule 12
Do not claim a build/test succeeded unless it actually succeeded.

### Rule 13
Do not silently implement future phases.

### Rule 14
After each phase, report changes and wait for approval.

---

# 54. REQUIRED FIRST RESPONSE

Your FIRST response after receiving this prompt must NOT contain implementation code.

It must contain:

## 1. Existing Project Analysis

- architecture
- project structure
- UI
- workflow
- geometry
- GIS integration
- validation
- output

## 2. Reusable Components

List what can be reused.

## 3. Required Modifications

List files/classes/components that should be modified.

## 4. New Components

List only components that genuinely need to be added.

## 5. Risks

List technical and architectural risks.

## 6. Proposed Architecture

Show how the new Parcel Builder integrates into the existing Add-in.

## 7. Implementation Phases

Provide the recommended sequence.

## 8. Explicit Confirmation

End with:

> Analysis complete. No files have been modified.  
> Awaiting approval to begin Phase 1.

Then STOP.

---

# 55. FINAL ENGINEERING PRINCIPLE

Build this as a **professional GIS engineering component**, not as a quick UI prototype.

The objective is not merely to display parcel rectangles.

The objective is to create a robust system in which:

```text
Existing GIS Data
       ↓
Analysis / Inference
       ↓
BlockConfiguration
       ↓
Editable User Configuration
       ↓
Geometry Engine
       ↓
Validation
       ↓
Schematic Preview
       ↓
GIS Preview
       ↓
Final Feature Class
```

is a coherent, deterministic, maintainable, testable, and extensible workflow.

The implementation must respect the existing ArcGIS Pro Add-in architecture and preserve existing functionality.

---

# 56. 13-STEP CAD/GIS STEPPER WORKFLOW DETAILED SPECIFICATION

The workstation guides the user through 13 sequential, highly coordinated steps:

### Step 1: Base Dimensions (الأبعاد الأساسية)
- Input: Standard Parcel Frontage ($F$) and Depth ($D$).
- Area is calculated automatically: $A = F \times D$.
- Live preview initializes with a single base parcel representation.

### Step 2: Parcel Relationship (طبيعة القطعة)
- Options:
  - `Standalone`: A single independent parcel.
  - `Part of Block`: Part of a larger cadastral block.

### Step 3: Block Arrangement (توزيع البلوك)
- Options:
  - `Single-Sided`: Parcels aligned along a single street frontage (Side A).
  - `Back-to-Back`: Dual rows sharing a common spine (Side A and Side B).
- Reference street designation (Street A or Street B).
- Custom vs. Equal parcel count per side toggle.

### Step 4: Parcel Count & Layout (أعداد وتوزيع القطع)
- Inputs: Parcel Count for Side A ($N_A$) and Parcel Count for Side B ($N_B$).
- Real-time computation of:
  - Total Frontage: $L_A = \sum F_{A,i}$, $L_B = \sum F_{B,i}$.
  - Total Area: $A_{block} = A_A + A_B$.
  - Block Dimensions: Length = $\max(L_A, L_B)$, Depth = $D_A + D_B$ (Back-to-Back) or $D_A$ (Single-Sided).

### Step 5: Dimension Similarity (تماثل الأبعاد)
- Options:
  - `All Parcels Identical`: All parcels inherit base frontage and depth.
  - `Some Parcels Different`: Allows setting individual overrides and exceptions.

### Step 6: Exceptions & Overrides (الاستثناءات والتعديلات الفردية)
- Interactive grid/table allowing individual adjustments per parcel:
  - Custom Frontage, Custom Depth, Custom Parcel Type (Commercial, Corner, Standard, Service).
- Bidirectional synchronization: Changes immediately update parcel coordinates, recalculate block boundaries, and re-render the schematic preview.

### Step 7: Corner & Chamfer (شطفات الأركان)
- Toggle: Enable/Disable corner chamfers.
- Chamfer Calculation Methods:
  - `By Chamfer Length`: Direct diagonal cut length.
  - `By Street Frontages`: Distance deducted from each intersecting street frontage.
- Selective application to: Outer corners (Start of Side A, End of Side A, Start of Side B, End of Side B).
- Real-time corner vertex clipping without producing invalid self-intersections or disjoint polygons.

### Step 8: Electric Room / Substation (غرفة الكهرباء والمحول)
- Comprehensive utility room allocation workflow:
  - Street side selection: `Side A` or `Side B`.
  - Dimensions: Width ($W_{ER}$), Depth ($D_{ER}$), Auto Area ($A_{ER} = W_{ER} \times D_{ER}$).
  - Offset distance along the street frontage (default: `0.0m`).
  - **Interactive Red Canvas Anchors**: Refined circular handles on street edges allowing 1-click snap placement.
  - **Five Anchor Point Modes**:
    - `Top-Left`: Room starts at anchor point and extends to the right.
    - `Top-Right`: Room ends at anchor point and extends to the left.
    - `Center`: Room centers symmetrically on the anchor point.
    - `Bottom-Left`: Bottom corner alignment.
    - `Bottom-Right`: Bottom corner alignment.
  - **Architectural Footprint Overlay**:
    - Vivid amber/gold highlight (`#FFA000`), technical dashed outline, and clear `⚡ ER` label.
    - Minimum screen rendering threshold (18×18 px) to ensure legibility across long blocks.
  - Subtractive geometry clipping from host parcel: `ER-01` parcel is created, host parcel frontage/depth/polygon are updated, and total area remains 100% conserved.

### Step 9: Parcel Split & Merge (فرز ودمج القطع)
- **Split Mode (فرز)**:
  - Select target parcel from interactive canvas or dropdown.
  - Split directions: `Along Frontage` (vertical split) or `Along Depth` (horizontal split).
  - Split methods: `Equal Split (50/50)`, `By Percentage`, `By Frontage Dimension`, `By Area`.
  - Outer corner preservation: If the target parcel has a corner chamfer, the chamfer is preserved exclusively on the outer corner child, while the inner child is cleanly squared off.
- **Merge Mode (دمج)**:
  - Select two parcels (Parcel 1 and Parcel 2) via canvas clicks or ID input.
  - **Same-Side Merge**: Merges adjacent parcels sharing a depth boundary into a wider unified parcel.
  - **Cross-Side / Spine Merge (الدمج بين الواجهتين / عبر الفاصل الخلفي)**:
    - User-controlled checkbox: `Allow Cross-Side / Spine Merge (السماح بالدمج بين الواجهتين / عبر الفاصل الخلفي)` (Default: `true`).
    - Validates shared boundary along the central spine ($Y = 0$) and matching frontage dimension.
    - Produces a unified **"Through Parcel"** spanning from Street A to Street B.
    - Automatically manages side list transfer (removes from Side B, replaces in Side A) and preserves row renumbering.
  - Exact area conservation ($Area_{merged} = Area_1 + Area_2$).

### Step 10: Spatial Alignment (المحاذاة المكانية)
- Georeferencing methods:
  - `Align by Existing Line`: Snap block frontage to an existing polyline/boundary in the active GIS map.
  - `Align by Two Points`: Define origin and orientation vector from map points.
  - `Manual Transformation`: Custom insertion point $(X, Y)$ and rotation angle ($\theta$).

### Step 11: Dynamic Preview (المعاينة المكانية والمخطط)
- Dual-layer visualization:
  - **Schematic WPF Canvas**: Fast, anti-aliased vector rendering with true aspect ratio, zoom/pan, hover tooltips, and interactive selection.
  - **ArcGIS Pro Map Overlay**: Live ephemeral graphics drawn via `MapView.Active.AddOverlay` with disposable CIM symbols. Zero Map Table of Contents (TOC) pollution.

### Step 12: Validation Rules (قواعد التحقق الهندسية والتنظيمية)
- Multi-tier validation engine:
  - Dimension compliance: Minimum frontage, depth, and area per planning regulations.
  - Geometric topology: Clean polygon closure, no self-intersections, no unintended gaps, no internal overlaps.
  - Boundary continuity: Complete shared edge alignment along adjacent parcel boundaries.
  - Status badges: Visual indicators (`Valid`, `Warning`, `Error`) with clear corrective instructions.

### Step 13: Final Review & Output (المراجعة النهائية والتوليد)
- Comprehensive block configuration audit and statistical summary.
- Geodatabase Workspace selector and Feature Class naming.
- Transactional GIS Feature Class generation using `EditOperation`:
  - Polyline/Polygon conversion with precise spatial reference (WGS84, UTM, or local projected CRS).
  - Rich cadastral attribute schema: `Parcel_ID`, `Side`, `Sequence`, `Frontage`, `Depth`, `Area`, `Type`, `HasChamfer`, `ChamferLength`, `IsModified`, `Notes`.
  - Automatic addition of the resulting layer to the active map with native Pro Undo/Redo integration.

---

# 57. INTERACTIVE ELECTRIC ROOM ARCHITECTURE & ANCHOR MODES

The Electric Room allocation engine in `ParcelGeometryEngine.cs` and `SchematicCanvasControl.cs` adheres to the following mathematical specifications:

1. **Precision Red Street Handles**:
   - Drawn on street boundary vertices and parcel junctions.
   - Geometry: Radius = $3.0\text{px}$, Hover radius = $4.5\text{px}$, Halo = $1.0\text{px}$.
   - Cursor changes to `Cursors.Hand` on hover.
   - Left-click dispatches `SelectElectricRoomLocationCommand(side, offset)` immediately.

2. **Anchor Translation Equations**:
   Given anchor coordinate $(X_a, Y_a)$ on the street frontage and room dimensions $(W, D)$:
   - **Top-Left (Default)**:
     $$X_{min} = X_a, \quad X_{max} = X_a + W$$
     $$Y_{min} = Y_a - D \text{ (Side A)}, \quad Y_{max} = Y_a + D \text{ (Side B)}$$
   - **Center**:
     $$X_{min} = X_a - \frac{W}{2}, \quad X_{max} = X_a + \frac{W}{2}$$
   - **Top-Right**:
     $$X_{min} = X_a - W, \quad X_{max} = X_a$$
   - **Bottom-Left / Bottom-Right**: Adjusted relative to back spine depth.

3. **Chamfer Offset Compensation**:
   When offset distance is $0.0\text{m}$, the room automatically snaps to the start of the valid rectangular frontage immediately following the corner chamfer cut, avoiding self-intersecting or clipped room boxes.

4. **Dedicated Schematic Footprint Overlay**:
   - Fill: `#FFA000` with 80% opacity.
   - Stroke: `#FFFFFF` (1.5px solid) + `#FFA000` (1.5px dashed).
   - Label: Centered bold text `⚡ ER`.
   - Cyan Anchor Marker: Glowing dot (`#00E5FF`) rendered at the active anchor location on the room footprint.

---

# 58. PARCEL SPLIT & MERGE SERVICE & CROSS-SIDE SPINE MERGING

The `ParcelMergeService` and `ParcelSplitService` provide mathematically rigorous parcel union and division:

1. **Winding Order Normalization (Counter-Clockwise CCW)**:
   - For any polygon ring, signed area is computed:
     $$A_{signed} = \frac{1}{2} \sum_{i=0}^{n-1} (x_i y_{i+1} - x_{i+1} y_i)$$
   - If $A_{signed} < 0$ (Clockwise), vertices are reversed to CCW.
   - **Why this is critical**: In Back-to-Back blocks, Side A parcels have depth in $+Y$ while Side B parcels have depth in $-Y$. Without CCW normalization, their shared boundary along the spine ($Y=0$) traverses in the *same* direction, breaking edge cancellation. Under CCW normalization, the seam edge runs in opposite directions ($(x_1, 0) \to (x_2, 0)$ vs $(x_2, 0) \to (x_1, 0)$), dissolving cleanly in `SubtractOverlappingEdge`.

2. **Bidirectional Edge Chaining (`ChainEdgesToPolygon`)**:
   - Chaining traverses remaining outer boundary edges.
   - If `e.From` does not match the active `current.To`, it searches for `e.To` and flips the edge dynamically.
   - Prevents jump-across artifacts, degenerate loops, and zero-area self-intersecting polygons.

3. **Cross-Side Through-Parcel Creation**:
   - When merging across sides:
     - Frontages must match along the spine: $|F_1 - F_2| \le 0.05\text{m}$.
     - Total Depth = $D_1 + D_2$.
     - Total Area = $Area_1 + Area_2$.
     - Resulting parcel type is set to `"Through Parcel"`.
     - Side list transfer: Removed from Side B, updated in Side A.
     - Row renumbering preserves `"Through Parcel"` without overwriting its classification to `"Standard"` or `"Corner"`.

---

# 59. STATE PERSISTENCE, BACKTRACKING & UNDO/REDO ENGINE

To prevent user frustration during multi-step configuration:

1. **State Retention on Backward Navigation**:
   - Moving backwards in the stepper (`← Back` or clicking previous step numbers) **MUST NOT** reset the model or wipe edits.
   - Previous steps reflect the current working state:
     - Parcel counts reflect actual generated count including splits and merges.
     - Exceptions table retains all custom dimensions.
     - Electric room parameters and split/merge histories remain intact.
2. **Deep-Clone Undo/Redo Stack (`PushState` / `UndoCommand`)**:
   - Destructive operations (`Split`, `Merge`, `ApplyElectricRoom`, `ResetExceptions`) invoke `PushState()` prior to mutation.
   - The user can click `Undo` or `Redo` at any point in the workflow to revert or re-apply mutations.
3. **Internal Sync Guard (`_isSyncingState`)**:
   - `SyncStateFromGeneratedParcels()` synchronizes counts, areas, and exception records from `GeneratedParcels` without triggering recursive geometric regeneration loops.

---

# 60. ARCGIS PRO RIBBON INTEGRATION & PACKAGING RULES

1. **Single Tab Ribbon Sanitation**:
   - The Add-In DAML (`Config.daml`) must define a single, clean ribbon tab:
     `<tab id="ParcelBuilder_Tab" caption="Parcel Builder">`
   - **Forbidden**: Do not declare or duplicate tools inside the default generic `<tab id="esri_core_addonTab">` or any other tab. The tool must appear exclusively in its dedicated `Parcel Builder` ribbon tab.
2. **Strict Manual Deployment Rule**:
   - Build scripts and output packaging must output exclusively to:
     `d:\Learning\PARCEL BUILDER\AddInPackage\ParcelBuilder.esriAddinX`
   - **Do NOT automatically copy or publish** the AddIn to the user's ArcGIS Pro system directory (`C:\Users\<user>\Documents\ArcGIS\AddIns\ArcGISPro\...`).
   - The user will perform the installation manually.

---

# 61. VERIFICATION & UNIT TESTING STANDARDS

1. All geometry algorithms, polygon clipping, chamfers, splits, merges, CCW winding normalization, and area calculations must have automated unit tests in `tests/ParcelBuilder.Tests`.
2. Regression prevention: Before concluding any task, execute:
   ```powershell
   dotnet test "tests/ParcelBuilder.Tests/ParcelBuilder.Tests.csproj"
   ```
   All tests (51/51 or more) must pass with zero failures.
3. Build verification: Execute:
   ```powershell
   dotnet build "ParcelBuilder.slnx"
   ```
   Must complete with 0 Warning(s) and 0 Error(s).
