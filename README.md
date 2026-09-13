# Parcel Builder — ArcGIS Pro Add-in
> **Enterprise Parcel and Block Builder Add-in for ArcGIS Pro (3.3.x, 3.4.x, and later)**  
> *Built with ArcGIS Pro SDK for .NET 8.0, C#, WPF, and MVVM Architecture.*

---

## 📖 Overview

**Parcel Builder** is an enterprise-grade cadastral and urban planning workstation seamlessly integrated into **ArcGIS Pro**. Built specifically for urban planners, surveyors, GIS professionals, and cadastre authorities, Parcel Builder flips traditional cadastre workflows on their head through a **Parcel-First Design Philosophy**.

Instead of manually drawing outer block perimeters and awkwardly attempting to carve parcels inside them, Parcel Builder constructs precision-engineered blocks from the individual parcel outwards, preserving mathematical, topological, and legal frontage consistency across the entire subdivision.

---

## 🌟 Key Features

- 📐 **Parcel-First Cadastral Engine**: Define standard parcel frontage and depth, arrange in single-sided or back-to-back blocks, and compute total block frontage, depth, and area with 100% geometric consistency.
- ⚡ **Electric Room / Substation Carving**: Seamlessly place and carve electrical substations (`ER-01`) along street frontages (corner, mid-block, custom offset, or map click) without losing block area or leaving slivers.
- ✂️ **Parcel Split & Merge Engine**:
  - **Split**: Divide any parcel along its frontage or depth into equal parts (2–5) or custom percentages, with automatic chamfer preservation.
  - **Merge**: Seamlessly union adjacent parcels along the same street frontage or fuse opposing parcels across the central spine into unified dual-frontage **Through Parcels**.
- 📍 **5 Interactive Map Tools**:
  1. **Select Parcel Tool**: Click existing GIS parcel polygons to infer frontage, depth, and surrounding block configuration.
  2. **Place Substation Tool**: Click any point along street frontage to snap and carve the electric room.
  3. **Set Base Point Tool**: Interactive map coordinate anchoring with real-time snapping.
  4. **2-Point Orientation Tool**: Click two points to define the block orientation baseline.
  5. **Pick Segment Tool**: Click any line or polygon boundary edge to instantly align block azimuth.
- 👁️ **Ephemeral Real-Time Overlays**: Zero Table-of-Contents (TOC) pollution with high-contrast, glowing vector overlays and instant centroid labels directly on the active MapView.
- 🛡️ **Comprehensive Topology Validation**: 9 automated cadastral topology checks:
  - No invalid geometries
  - No gaps
  - No overlaps
  - No duplicate polygons
  - No multipart geometries
  - Minimum segment length threshold (> 10 cm)
  - Acute angle warning (< 5°)
  - Vertex proximity / snapping check (< 1 cm)
  - T-junction node vertex detection
- 🗄️ **Enterprise Geodatabase Export**: Generates File Geodatabase feature classes with full cadastre attribute schemas (`ParcelID`, `FeatureType`, `Side`, `Sequence`, `Frontage`, `FrontageA`, `FrontageB`, `Depth`, `Area_sqm`, `ParcelType`, `IsCorner`, `StreetLabel`, `PlacementType`, `BlockLength`, `Arrangement`).
- 🌐 **Bilingual UI & High-Contrast Design**: Complete English and Arabic (RTL) interface with WCAG AAA compliant dark theme for GIS workstations.

---

## 🖥️ System Requirements & Compatibility

| Component | Requirement |
|---|---|
| **Host Application** | **ArcGIS Pro 3.3.x, 3.4.x, or later (3.x)** |
| **Runtime Target** | **.NET 8.0 Desktop Runtime** (`net8.0-windows`) |
| **Operating System** | Windows 10 / Windows 11 (64-bit) |
| **Architecture** | x64 |

---

## 🚀 Installation

### Option 1: Double-Click Install (Pre-packaged Add-In)

1. Ensure **ArcGIS Pro** is closed.
2. Locate the packaged add-in at:
   ```text
   AddInPackage\ParcelBuilder.esriAddinX
   ```
3. Double-click **`ParcelBuilder.esriAddinX`**.
4. In the **Esri ArcGIS Pro Add-In Installation Utility** dialog, click **Install Add-In**.
5. Launch ArcGIS Pro and open any map.

### Option 2: Build from Source

You can build and package the solution using the .NET CLI or Visual Studio:

```bash
# Build the Release package (.NET 8)
dotnet build ParcelBuilder.slnx -c Release
```

Upon a successful build, the add-in package will be automatically generated at:
```text
AddInPackage\ParcelBuilder.esriAddinX
```

---

## 🕹️ 13-Step Guided Workflow

```text
[1. Base Dimensions] ➔ [2. Relationship]   ➔ [3. Arrangement]  ➔ [4. Parcel Count & Layout]
        ↓
[5. Similarity]      ➔ [6. Exceptions]     ➔ [7. Chamfers]     ➔ [8. Electric Room]
        ↓
[9. Split & Merge]   ➔ [10. Alignment]     ➔ [11. Preview]     ➔ [12. Validation]
        ↓
[13. Final Review & GDB Export]
```

1. **Base Dimensions**: Set standard parcel frontage ($m$), depth ($m$), and calculate base area ($m^2$).
2. **Parcel Relationship**: Choose between a standalone parcel or part of a multi-parcel urban block.
3. **Block Arrangement**: Choose single-sided frontage (Street A) or back-to-back dual frontages (Street A & Street B).
4. **Parcel Count & Layout**: Set individual parcel counts per street side with live cumulative frontage stats.
5. **Dimension Similarity**: Choose uniform parcel dimensions or enable individual parcel overrides.
6. **Exceptions & Overrides**: Override specific parcels with custom frontages, depths, or land-use types.
7. **Corner & Chamfer**: Apply splayed corner chamfers by length or street setbacks with live preview.
8. **Electric Room / Substation**: Place and carve an electric room (`ER-01`) with automatic host parcel area adjustment.
9. **Parcel Split & Merge**:
   - Split selected parcels into sub-parcels (2 to 5 parts or custom ratios).
   - Merge contiguous parcels along the same street side or cross-spine into unified dual-frontage through parcels.
10. **Spatial Alignment**: Anchor block coordinates via base point snapping, 2-point vector drawing, or segment picking.
11. **Dynamic Preview**: Inspect the CAD schematic canvas and toggle live, zero-TOC MapView overlays.
12. **Topology Validation**: Execute 9 topological rules with instant visual status feedback.
13. **Final Review & Output**: Export the validated layout directly to a File Geodatabase Polygon Feature Class.

---

## 🏛️ Project Architecture

```text
PARCEL BUILDER/
├── AddInPackage/                     # Distribution package directory
│   └── ParcelBuilder.esriAddinX      # Ready-to-install ArcGIS Pro Add-In
├── dev/                              # Design documents & feature specifications
│   ├── dev.md                        # UI/UX and feature requirements log
│   └── ele.md                        # Electric room technical specifications
├── src/
│   ├── Core/                         # ParcelBuilder.Core (.NET 8 class library)
│   │   ├── Geometry/                 # Computational geometry engines
│   │   │   ├── ParcelGeometryEngine.cs
│   │   │   ├── ParcelSplitService.cs
│   │   │   ├── ParcelMergeService.cs
│   │   │   ├── BlockTransformationService.cs
│   │   │   └── TopologyValidationEngine.cs
│   │   └── Models/                   # Domain models and configuration entities
│   │       ├── BlockConfiguration.cs
│   │       ├── ParcelModel.cs
│   │       ├── ElectricRoomConfiguration.cs
│   │       └── Enums.cs
│   └── AddIn/                        # ParcelBuilder.AddIn (ArcGIS Pro SDK Add-in)
│       ├── Config.daml               # Desktop Application Markup Language declaration
│       ├── ParcelBuilderModule.cs    # ArcGIS Pro module entry point
│       ├── ParcelBuilderDockPaneView.xaml # 13-step dockpane UI (WPF / XAML)
│       ├── ParcelBuilderDockPaneViewModel.cs # MVVM view model
│       ├── Controls/                 # Custom controls (SchematicCanvasControl)
│       ├── Services/                 # Map preview & Feature Class export services
│       │   ├── ParcelPreviewService.cs
│       │   └── ParcelFeatureClassService.cs
│       └── MapTools/                 # Interactive ArcGIS Pro sketch map tools
│           ├── SelectParcelTool.cs
│           ├── SelectElectricRoomLocationTool.cs
│           ├── SelectBasePointTool.cs
│           ├── AlignmentTool.cs
│           └── SelectAlignmentFeatureTool.cs
└── tests/                            # Automated unit test suite (.NET 8 / xUnit)
    └── ParcelBuilder.Tests/
```

---

## 📋 Cadastre Feature Class Schema

When exporting to Geodatabase, each feature is written with complete cadastral attributes:

| Field Name | Type | Length | Description |
|---|---|---|---|
| `ParcelID` | TEXT | 30 | Unique parcel identifier (e.g. `A-01`, `B-03`, `ER-01`, `A-02/B-02`) |
| `FeatureType` | TEXT | 30 | Classification (`Parcel`, `ElectricRoom`, `ThroughParcel`) |
| `Side` | TEXT | 20 | Street side (`SideA`, `SideB`, `Side A & Side B`) |
| `Sequence` | LONG | - | Sequence number along block frontage |
| `Frontage` | DOUBLE | - | Primary frontage width ($m$) |
| `FrontageA` | DOUBLE | - | Frontage along Street A ($m$) |
| `FrontageB` | DOUBLE | - | Frontage along Street B ($m$) |
| `Depth` | DOUBLE | - | Parcel depth ($m$) |
| `Area_sqm` | DOUBLE | - | Calculated polygon area ($m^2$) |
| `ParcelType` | TEXT | 30 | Typology (`Standard`, `Corner`, `Electric Room`, `Through Parcel`) |
| `IsCorner` | SHORT | - | Corner parcel flag (`1` or `0`) |
| `StreetLabel` | TEXT | 100 | Street frontage name/label |
| `PlacementType`| TEXT | 30 | Electric room placement type |
| `HostParcel1` | TEXT | 30 | Primary host parcel ID for substation |
| `HostParcel2` | TEXT | 30 | Secondary host parcel ID for substation |
| `BlockLength` | DOUBLE | - | Total calculated block length ($m$) |
| `Arrangement` | TEXT | 30 | Block arrangement mode (`SingleSided` or `BackToBack`) |

---

## 📄 Documentation

- [User Guide (دليل المستخدم الشامل)](file:///d:/Learning/PARCEL%20BUILDER/USER_GUIDE.md) — Complete step-by-step tutorial with screenshots and cadastral guidelines.
- [Developer Requirements](file:///d:/Learning/PARCEL%20BUILDER/dev/dev.md) — Implementation verification checklist.
- [Electric Room Specification](file:///d:/Learning/PARCEL%20BUILDER/dev/ele.md) — Substation placement and spatial carving details.

---

## ⚖️ License & Authors

- **Created by Mahmoud Aboelmagd with AI**
- **Repository**: [aboelmagd1/ParcelBlockBuilder](https://github.com/aboelmagd1/ParcelBlockBuilder)
