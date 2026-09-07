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
    /// Method used to align the generated block in spatial coordinates.
    /// </summary>
    public enum AlignmentMethod
    {
        TwoPoints = 0,
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
        SideB = 1
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
