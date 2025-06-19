namespace ValorVDC_BIMTools.Utilities;

public class FabricationPartAdjustmentUtility
{
    public static bool AdjustFabricationPartLength(FabricationPart fabricationPart, double desiredLength,
        XYZ pickedPoint, Document document)
    {
        if (fabricationPart?.Location is not LocationCurve locationCurve ||
            locationCurve.Curve is not Line currentLine)
            return false;

        if (!IsFabricationDuctOrPipe(fabricationPart.Category.Id)) return false;

        var currentLength = currentLine.Length;
        var adjustmentLength = desiredLength - currentLength;

        if (Math.Abs(adjustmentLength) < 0.001) return true; // Already at desired length
        var connectors = fabricationPart.ConnectorManager.Connectors;
        Connector connector0 = null;
        Connector connector1 = null;

        foreach (Connector connector in connectors)
            if (connector.ConnectorType == ConnectorType.End)
            {
                if (connector.Id == 0)
                    connector0 = connector;
                else if (connector.Id == 1)
                    connector1 = connector;
            }

        if (connector0 == null || connector1 == null) return false;

        var distanceToConnector0 = pickedPoint.DistanceTo(connector0.Origin);
        var distanceToConnector1 = pickedPoint.DistanceTo(connector1.Origin);

        Connector connectorToAdjust;
        Connector fixedConnector;
        var bothUnconnected = !connector0.IsConnected && !connector1.IsConnected;

        if (bothUnconnected)
        {
            if (distanceToConnector0 <= distanceToConnector1)
            {
                connectorToAdjust = connector0;
                fixedConnector = connector1;
            }
            else
            {
                connectorToAdjust = connector1;
                fixedConnector = connector0;
            }
        }
        else
        {
            connectorToAdjust = distanceToConnector0 < distanceToConnector1 ? connector1 : connector0;
            fixedConnector = connectorToAdjust == connector0 ? connector1 : connector0;
        }

        using (var transaction = new Transaction(document, "Adjust Fabrication Part Length"))
        {
            transaction.Start();

            var adjustmentSuccessful = false;

            var lengthParam = fabricationPart.get_Parameter(BuiltInParameter.FABRICATION_PART_LENGTH);
            if (lengthParam != null && !lengthParam.IsReadOnly)
                try
                {
                    lengthParam.Set(desiredLength);
                    adjustmentSuccessful = true;
                }
                catch (Exception ex)
                {
                    // Length parameter adjustment failed, continue to other methods
                }

            // If parameter method failed, try AdjustEndLength
            if (!adjustmentSuccessful)
                adjustmentSuccessful = TryFabricationAdjustment(fabricationPart, connectorToAdjust, adjustmentLength) ||
                                       TryFabricationAdjustment(fabricationPart, fixedConnector, adjustmentLength);

            // Fallback to LocationCurve method if Fabrication methods fail
            if (!adjustmentSuccessful)
            {
                var (adjustedLine, adjustmentDelta) =
                    AdjustCurve(locationCurve, connectorToAdjust, fixedConnector, adjustmentLength);

                if (adjustedLine != null)
                    try
                    {
                        locationCurve.Curve = adjustedLine;

                        if (connectorToAdjust.IsConnected)
                            foreach (Connector connectedConnector in connectorToAdjust.AllRefs)
                                if (connectedConnector.Owner.Id != fabricationPart.Id)
                                {
                                    var connectedElement = document.GetElement(connectedConnector.Owner.Id);
                                    if (connectedElement != null)
                                    {
                                        var locationPoint = connectedElement.Location as LocationPoint;
                                        if (locationPoint != null)
                                        {
                                            var currentPoint = locationPoint.Point;
                                            locationPoint.Point = currentPoint + adjustmentDelta;
                                        }
                                        else
                                        {
                                            ElementTransformUtils.MoveElement(document, connectedElement.Id,
                                                adjustmentDelta);
                                        }
                                    }
                                }

                        adjustmentSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        // LocationCurve adjustment also failed
                    }
            }

            if (adjustmentSuccessful)
            {
                document.Regenerate();
                transaction.Commit();
                return true;
            }

            transaction.RollBack();
            return false;
        }
    }

    private static bool TryFabricationAdjustment(FabricationPart fabricationPart, Connector connector,
        double adjustmentLength)
    {
        try
        {
            fabricationPart.AdjustEndLength(connector, adjustmentLength, false);
            return true;
        }
        catch
        {
            try
            {
                fabricationPart.AdjustEndLength(connector, adjustmentLength, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private static (Line adjustedLine, XYZ adjustmentDelta) AdjustCurve(LocationCurve locationCurve,
        Connector connectorToAdjust, Connector fixedConnector, double adjustmentLength)
    {
        if (locationCurve == null || connectorToAdjust == null || fixedConnector == null)
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

    private static bool IsFabricationDuctOrPipe(ElementId categoryId)
    {
        return categoryId.Value == (int)BuiltInCategory.OST_FabricationDuctwork ||
               categoryId.Value == (int)BuiltInCategory.OST_FabricationPipework ||
               categoryId.Value == (int)BuiltInCategory.OST_FabricationHangers;
    }
}