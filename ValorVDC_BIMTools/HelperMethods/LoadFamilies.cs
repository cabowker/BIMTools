using System.IO;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace ValorVDC_BIMTools.HelperMethods;

public class LoadFamilies
{
    /// <summary>
    ///     Load a family from a specific file path
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="uiDocument">The active UI document (optional, for view refresh)</param>
    /// <param name="familyPath">Path to the family file</param>
    /// <param name="transactionName">Name for the transaction</param>
    /// <param name="activateSymbols">Whether to activate family symbols</param>
    /// <returns>The loaded family or null if loading failed</returns>
    public static Family LoadFamilyFromPath(
        Document document,
        UIDocument uiDocument,
        string familyPath,
        string transactionName = "Load Family",
        bool activateSymbols = true)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (string.IsNullOrEmpty(familyPath))
            throw new ArgumentNullException(nameof(familyPath));

        if (!File.Exists(familyPath))
            return null;

        Family loadedFamily = null;

        using (var transaction = new Transaction(document, transactionName))
        {
            transaction.Start();

            try
            {
                // Try to load the family
                var success = document.LoadFamily(familyPath, out loadedFamily);

                if (success && loadedFamily != null)
                {
                    var symbolIds = loadedFamily.GetFamilySymbolIds();

                    if (symbolIds.Count > 0 && activateSymbols)
                        foreach (var symbolId in symbolIds)
                        {
                            var symbol = document.GetElement(symbolId) as FamilySymbol;
                            if (symbol != null && !symbol.IsActive) symbol.Activate();
                        }

                    document.Regenerate();
                    transaction.Commit();

                    uiDocument?.RefreshActiveView();

                    Thread.Sleep(100);
                }
                else
                {
                    transaction.RollBack();
                    loadedFamily = null;
                }
            }
            catch (Exception)
            {
                transaction.RollBack();
                loadedFamily = null;
                throw; // Re-throw to allow caller to handle the exception
            }
        }

        return loadedFamily;
    }

    /// <summary>
    ///     Load a family from the default location
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="uiDocument">The active UI document (optional, for view refresh)</param>
    /// <param name="defaultPath">Default path to the family file</param>
    /// <param name="transactionName">Name for the transaction</param>
    /// <returns>The loaded family or null if loading failed</returns>
    public static Family LoadDefaultFamily(
        Document document,
        UIDocument uiDocument,
        string defaultPath,
        string transactionName = "Load Default Family")
    {
        if (!File.Exists(defaultPath))
            return null;

        return LoadFamilyFromPath(document, uiDocument, defaultPath, transactionName);
    }

    /// <summary>
    ///     Show a dialog to browse for a family file and load it
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="uiDocument">The active UI document (optional, for view refresh)</param>
    /// <param name="dialogTitle">Title for the file dialog</param>
    /// <param name="transactionName">Name for the transaction</param>
    /// <returns>The loaded family or null if loading was cancelled or failed</returns>
    public static Family BrowseAndLoadFamily(
        Document document,
        UIDocument uiDocument,
        string dialogTitle = "Select Family",
        string transactionName = "Load Family")
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = dialogTitle,
            Filter = "Revit Family Files (*.rfa)|*.rfa",
            FilterIndex = 1,
            RestoreDirectory = true
        };

        if (openFileDialog.ShowDialog() == true)
            return LoadFamilyFromPath(document, uiDocument, openFileDialog.FileName, transactionName);

        return null;
    }

    /// <summary>
    ///     Find a family in the document by name
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="familyName">Name of the family to find</param>
    /// <returns>The family if found, otherwise null</returns>
    public static Family FindFamilyByName(Document document, string familyName)
    {
        if (document == null || string.IsNullOrEmpty(familyName))
            return null;

        using (var collector = new FilteredElementCollector(document))
        {
            var families = collector.OfClass(typeof(Family)).Cast<Family>();
            return families.FirstOrDefault(f => f.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    ///     Get the symbols for a specific family
    /// </summary>
    /// <param name="document">The active Revit document</param>
    /// <param name="family">The family to get symbols from</param>
    /// <returns>Array of family symbols</returns>
    public static FamilySymbol[] GetFamilySymbols(Document document, Family family)
    {
        if (document == null || family == null)
            return new FamilySymbol[0];

        var symbolIds = family.GetFamilySymbolIds();
        var symbols = new List<FamilySymbol>();

        foreach (var symbolId in symbolIds)
        {
            var symbol = document.GetElement(symbolId) as FamilySymbol;
            if (symbol != null) symbols.Add(symbol);
        }

        return symbols.ToArray();
    }
    
    public static bool LoadDefaultFloorSleeveFamily(Document document)
    {
        try
        {
            string defaultPath = @"C:\ProgramData\ValorVDC\Families\SLEEVE - Pipe Floor Sleeve.rfa";

            string directory = Path.GetDirectoryName(defaultPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(defaultPath))
            {
                TaskDialog.Show("Default Family Not Found", 
                    $"The default floor sleeve family was not found at:\n{defaultPath}\n\nPlease use the Browse button to locate it manually.");
                return false;
            }

            var uiDocument = new UIDocument(document);
            var family = LoadFamilyFromPath(document, uiDocument, defaultPath, "Load Default Floor Sleeve Family");
        
            if (family != null)
            {
                TaskDialog.Show("Success", "Default floor sleeve family loaded successfully.");
                return true;
            }
        
            TaskDialog.Show("Error", "Failed to load the default floor sleeve family.");
            return false;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"Error loading default floor sleeve family: {ex.Message}");
            return false;
        }

    }

    public static bool LoadFloorSleeveFamily(Document document)
    {
        try
        {
            var uiDocument = new UIDocument(document);
            var family = BrowseAndLoadFamily(document, uiDocument, "Select Floor Sleeve Family", "Load Floor Sleeve Family");
        
            if (family != null)
            {
                return true;
            }
        
            return false;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"Error loading floor sleeve family: {ex.Message}");
            return false;
        }
    }

}