namespace ValorVDC_BIMTools.HelperMethods;

public class CurveMethods
{
    /// <summary>
    ///     Gets the host level and calculates the placement point based on the pipe and reference
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="element">The pipe element</param>
    /// <param name="line">The line representing the pipe</param>
    /// <param name="reference">The reference from the user selection</param>
    /// <returns>A tuple containing the host level and placement point</returns>
    public (Level hostLevel, XYZ placementPoint) GetLevelAndPlacementPoint(Document document, Element element,
        Line line, Reference reference)
    {
        // Get the level from the pipe
        var levelId = element.LevelId;
        var hostLevel = document.GetElement(levelId) as Level;

        // Get the X,Y coordinates from the click point but use the host level's elevation
        var clickPoint = reference.GlobalPoint;
        var pointOnPipe = line.Project(clickPoint).XYZPoint;
        var placementPoint = new XYZ(
            pointOnPipe.X,
            pointOnPipe.Y,
            pointOnPipe.Z - (hostLevel?.Elevation ?? 0));

        return (hostLevel, placementPoint);
    }


    /// <summary>
    ///     Aligns an arrow family instance with the direction of a pipe
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="familyInstance">The arrow family instance to align</param>
    /// <param name="line">The line representing the pipe direction</param>
    /// <param name="placementPoint">The point where the arrow is placed</param>
    public void AlignElementWithCurve(Document document, FamilyInstance familyInstance, Line line, XYZ placementPoint)
    {
        var pipeDirection = line.Direction.Normalize();

        var location = familyInstance.Location as LocationPoint;
        if (location == null) return;
        var targetAngle = Math.Atan2(pipeDirection.Y, pipeDirection.X);
        var currentAngle = location.Rotation;
        var rotationNeeded = targetAngle - currentAngle;

        while (rotationNeeded > Math.PI) rotationNeeded -= 2 * Math.PI;
        while (rotationNeeded < -Math.PI) rotationNeeded += 2 * Math.PI;

        if (Math.Abs(rotationNeeded) > 0.001) // Only rotate if there's a significant difference
        {
            var rotationAxis = Line.CreateBound(placementPoint, placementPoint + XYZ.BasisZ);
            location.Rotate(rotationAxis, rotationNeeded);
        }
    }
}