namespace ValorVDC_BIMTools.Utilities;

public static class MepCurveAdjustmentUtility
{
    public static bool AdjustMepCurveLength(MEPCurve mepCurve, double desiredLength, XYZ pickedPoint, Document document)
    {
        if (mepCurve?.Location is not LocationCurve locationCurve ||
            locationCurve.Curve is not Line currentLine)
            return false;

        var currentLength = currentLine.Length;
        var adjustmentLength = desiredLength - currentLength;

        if (Math.Abs(adjustmentLength) < 0.001) return true; // Already at desired length

        var connectors = mepCurve.ConnectorManager.Connectors;
        Connector connector0 = null;
        Connector connector1 = null;

        foreach (Connector connector in connectors)
            if (connector.Id == 0)
                connector0 = connector;
            else if (connector.Id == 1)
                connector1 = connector;

        if (connector0 == null || connector1 == null) return false;

        var distanceToConnector0 = pickedPoint.DistanceTo(connector0.Origin);
        var distanceToConnector1 = pickedPoint.DistanceTo(connector1.Origin);

        var connectorToExtend = distanceToConnector0 < distanceToConnector1 ? connector1 : connector0;
        var oppositeConnector = connectorToExtend == connector0 ? connector1 : connector0;

        var (adjustedLine, adjustmentDelta) =
            AdjustCurve(locationCurve, connectorToExtend, oppositeConnector, adjustmentLength);

        if (adjustedLine == null) return false;

        using (var transaction = new Transaction(document, "Adjust MEP Curve Length"))
        {
            transaction.Start();

            locationCurve.Curve = adjustedLine;

            // Move connected elements
            foreach (Connector connector in connectorToExtend.AllRefs)
                if (connector.Owner.Id != mepCurve.Id)
                {
                    var connectedElement = document.GetElement(connector.Owner.Id);
                    if (connectedElement != null)
                    {
                        var location = connectedElement.Location as LocationPoint;
                        if (location != null)
                        {
                            var currentPoint = location.Point;
                            location.Point = currentPoint + adjustmentDelta;
                        }
                    }
                }

            transaction.Commit();
        }

        return true;
    }

    private static (Line adjustedLine, XYZ adjustmentDelta) AdjustCurve(LocationCurve locationCurve,
        Connector connectorToAdjust, Connector oppositeConnector, double adjustmentLength)
    {
        if (locationCurve == null || connectorToAdjust == null || oppositeConnector == null)
            return (null, null);

        var currentLine = locationCurve.Curve as Line;
        if (currentLine == null)
            return (null, null);

        var direction = (currentLine.GetEndPoint(1) - currentLine.GetEndPoint(0)).Normalize();
        var newStartPoint = currentLine.GetEndPoint(0);
        var newEndPoint = currentLine.GetEndPoint(1);
        var adjustmentDelta = XYZ.Zero;

        if (adjustmentLength > 0) // Lengthening
        {
            if (connectorToAdjust.Id == 0)
            {
                adjustmentDelta = -direction * adjustmentLength;
                newStartPoint = newStartPoint + adjustmentDelta;
            }
            else
            {
                adjustmentDelta = direction * adjustmentLength;
                newEndPoint = newEndPoint + adjustmentDelta;
            }
        }
        else // Shortening
        {
            if (connectorToAdjust.Id == 0)
            {
                adjustmentDelta = direction * Math.Abs(adjustmentLength);
                newStartPoint = newStartPoint + adjustmentDelta;
            }
            else
            {
                adjustmentDelta = -direction * Math.Abs(adjustmentLength);
                newEndPoint = newEndPoint - direction * Math.Abs(adjustmentLength);
            }
        }

        return (Line.CreateBound(newStartPoint, newEndPoint), adjustmentDelta);
    }
}