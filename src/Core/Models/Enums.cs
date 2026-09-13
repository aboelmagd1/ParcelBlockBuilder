namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Identifies how the block configuration was initiated.
    /// </summary>
    public enum SourceMode
    {
        Manual = 0,
        ExtractedFromExistingParcel = 1
    }

    /// <summary>
    /// Identifies whether parcels are arranged in a single row or back-to-back dual rows.
    /// </summary>
    public enum ArrangementMode
    {
        SingleSided = 0,
        BackToBack = 1
    }

    /// <summary>
    /// Identifies whether the configuration describes a standalone parcel or a block assembly.
    /// </summary>
    public enum RelationshipType
    {
        Standalone = 0,
        PartOfBlock = 1
    }

    /// <summary>
    /// Identifies if all parcels inherit base dimensions or if specific exceptions exist.
    /// </summary>
    public enum SimilarityMode
    {
        AllIdentical = 0,
        HasExceptions = 1
    }

    /// <summary>
    /// Type of corner parcel modification applied.
    /// </summary>
    public enum CornerType
    {
        None = 0,
        ChamferByLength = 1,
        ChamferByFrontage = 2
    }

    /// <summary>
    /// Specific calculation method for corner chamfers.
    /// </summary>
    public enum ChamferMode
    {
        DirectCutLength = 0,        // 1- Direct cut length along chamfer diagonal
        StreetSetbacks = 1,         // 2- Setback distances along intersecting street edges (auto-calculates chamfer length)
        CutLengthAndAngle = 2,      // 3- Chamfer cut length with specified cut angle
        SetbackAndAngle = 3         // 4- Setback distance on primary edge with specified cut angle
    }

    /// <summary>
    /// Placement position mode for the electric room / substation.
    /// </summary>
    public enum ElectricRoomPlacementMode
    {
        AtOffsetDistance = 0,       // Positioned at X meters from the start of the block frontage
        OnSpecificParcel = 1,       // Positioned within a specific parcel
        BetweenParcels = 2          // Positioned at the boundary between two adjacent parcels
    }

    /// <summary>
    /// Placement relationship type for the electric room footprint.
    /// </summary>
    public enum ElectricRoomPlacementType
    {
        InsideSingleParcel = 0,     // Electric room footprint is entirely inside one host parcel
        BetweenTwoParcels = 1,      // Electric room footprint spans across the boundary between two parcels
        InvalidOutsideBlock = 2     // Footprint extends outside block boundary or invalid
    }

    /// <summary>
    /// Placement method used to define the electric room position.
    /// </summary>
    public enum ElectricRoomPlacementMethod
    {
        InteractiveMapPlacement = 0, // Click location interactively on ArcGIS Pro map
        OffsetDistance = 1           // Specify offset distance along street frontage
    }

    /// <summary>
    /// Method used to align the generated block in spatial coordinates.
    /// </summary>
    public enum AlignmentMethod
    {
        TwoPoints = 0,
        MapSegment = 1,
        ExistingLine = 1,
        ExplicitAngle = 2
    }

    /// <summary>
    /// Confidence and state status for inferred properties during extraction.
    /// </summary>
    public enum ExtractionStatus
    {
        NotExtracted = 0,
        Detected = 1,
        Estimated = 2,
        Ambiguous = 3,
        Failed = 4
    }

    /// <summary>
    /// Validation message severity.
    /// </summary>
    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// Specifies which side of a block a parcel belongs to.
    /// </summary>
    public enum ParcelSide
    {
        SideA = 0,
        SideB = 1,
        Both = 2
    }

    /// <summary>
    /// Specifies the anchor reference point on the block used for spatial alignment.
    /// </summary>
    public enum BlockAnchorPoint
    {
        SideAStart = 0,    // Front-Left (P1 at X=0, Y=0)
        SideAEnd = 1,      // Front-Right (P1 at X=TotalFrontage, Y=0)
        SideACenter = 2,   // Midpoint of Side A Frontage
        SideBStart = 3,    // Back-Left (P1 at X=0 on Side B)
        SideBEnd = 4,      // Back-Right (P1 at X=TotalFrontage on Side B)
        BlockCenter = 5    // Centroid / Center of Block
    }
}
