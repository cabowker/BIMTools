namespace ValorVDC_BIMTools.HelperMethods;

public class CurveMethods
{
    /// <summary>
    /// Gets the host level and calculates the placement point based on the pipe and reference
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="element">The pipe element</param>
    /// <param name="line">The line representing the pipe</param>
    /// <param name="reference">The reference from the user selection</param>
    /// <returns>A tuple containing the host level and placement point</returns>
    public (Level hostLevel, XYZ placementPoint) GetLevelAndPlacementPoint(Document document, Element element, Line line, Reference reference)
    {
        // Get the level from the pipe
        ElementId levelId = element.LevelId;
        Level hostLevel = document.GetElement(levelId) as Level;
            
        // Get the X,Y coordinates from the click point but use the host level's elevation
        XYZ clickPoint = reference.GlobalPoint;
        XYZ pointOnPipe = line.Project(clickPoint).XYZPoint;
        XYZ placementPoint = new XYZ(
            pointOnPipe.X,
            pointOnPipe.Y,
            pointOnPipe.Z - (hostLevel?.Elevation ?? 0));
                
        return (hostLevel, placementPoint);
    }

    
    /// <summary>
    /// Aligns an arrow family instance with the direction of a pipe
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="familyInstance">The arrow family instance to align</param>
    /// <param name="line">The line representing the pipe direction</param>
    /// <param name="placementPoint">The point where the arrow is placed</param>

    public void AlignElementWithCurve(Document document, FamilyInstance familyInstance, Line line, XYZ placementPoint)
    {
        XYZ pipeDirection = line.Direction.Normalize();

        // Create a transform that aligns with the pipe direction
        Transform transform = Transform.Identity;
        transform.Origin = placementPoint;
        transform.BasisX = pipeDirection;
        transform.BasisY = XYZ.BasisZ.CrossProduct(pipeDirection).Normalize();
        transform.BasisZ = XYZ.BasisZ;

        // Apply the transform after placement
        ElementTransformUtils.RotateElement(document, familyInstance.Id, 
            Line.CreateBound(placementPoint, placementPoint + XYZ.BasisZ), 
            Math.Atan2(pipeDirection.Y, pipeDirection.X) + Math.PI);
    }

}