using System.Collections.ObjectModel;
using System.Windows;


namespace ValorVDC_BIMTools.Commands.CopyScopeBoxes.ViewModels;

public class LinkedModel : ObservableObject
{
    public string Name { get; set; }
    public RevitLinkInstance LinkInstance { get; set; }
}

public class ScopeBoxItem : ObservableObject
{
    private bool _isSelected;
    private string _name;
    private Element _element;

    private readonly ScopeBoxManagerViewModel _parentViewModel;

    public ScopeBoxItem(ScopeBoxManagerViewModel parentViewModel = null)
    {
        _parentViewModel = parentViewModel;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set 
        {
            SetProperty(ref _isSelected, value);
            // Notify parent view model that selection changed
            _parentViewModel?.UpdateCommandState();
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Element Element
    {
        get => _element;
        set => SetProperty(ref _element, value);
    }
    
    public override string ToString()
    {
        return $"ScopeBoxItem: {Name}, Selected: {IsSelected}";
    }

}

public class ScopeBoxManagerViewModel : ObservableObject
{
    private readonly Document _document;
    private readonly Window _parentWindow;
    private LinkedModel _selectedLinkedModel;
    private bool _disposed = false;

    public ObservableCollection<LinkedModel> LinkModels { get; }
    public ObservableCollection<ScopeBoxItem> ScopeBoxes { get; }
    
    public LinkedModel SelectedLinkedModel
    {
        get => _selectedLinkedModel;
        set
        {
            SetProperty(ref _selectedLinkedModel, value);
            UpdateScopeBoxes();
        }
    }

    public IRelayCommand CopyScopeBoxesCommand { get; }
    public IRelayCommand CloseCommand { get; }


    
    public ScopeBoxManagerViewModel(Document document, Window parentWindow = null)
    {
        _document = document;
        _parentWindow = parentWindow;
        LinkModels = new ObservableCollection<LinkedModel>();
        ScopeBoxes = new ObservableCollection<ScopeBoxItem>();
        
        CopyScopeBoxesCommand = new RelayCommand(CopyScopeBoxes, CanCopyScopeBoxes);
        CloseCommand = new RelayCommand(() => _parentWindow?.Close());


        LoadLinkedModels();
    }
    
    private void LoadLinkedModels()
    {
        using var collector = new FilteredElementCollector(_document);
        var linkInstances = collector
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .ToList();


        foreach (var linkInstance in linkInstances)
        {
            LinkModels.Add(new LinkedModel
            {
                Name = linkInstance.Name,
                LinkInstance = linkInstance
            });
        }
    }

    private void UpdateScopeBoxes()
    {
        try
        {
            ScopeBoxes.Clear();
            if (SelectedLinkedModel?.LinkInstance == null)
            {
                return;
            }

            var linkedDocument = SelectedLinkedModel.LinkInstance.GetLinkDocument();
            if (linkedDocument == null)
            {
                MessageBox.Show("Failed to get linked document. The link might be unloaded.", "Debug Info");
                return;
            }
            
            using var collector = new FilteredElementCollector(linkedDocument);
            var scopeBoxElements = collector
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .WhereElementIsNotElementType()
                .ToElements();
            
            foreach (var element in scopeBoxElements)
            {
                ScopeBoxes.Add(new ScopeBoxItem(this) 
                {
                    Name = element.Name,
                    Element = element,
                    IsSelected = false
                });
            }
            
            if (!scopeBoxElements.Any())
            {
                MessageBox.Show("No scope boxes found in the selected model.", "Information");
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error loading scope boxes: {e.Message}", "Error");
        }

        CopyScopeBoxesCommand?.NotifyCanExecuteChanged();
    }

    private void CopyScopeBoxes()
    {
        if (SelectedLinkedModel?.LinkInstance == null)
        {
            MessageBox.Show("No linked model selected.", "Information");
            return;
        }

        var selectedScopeBoxes = ScopeBoxes.Where(sb => sb.IsSelected).ToList();
        if (!selectedScopeBoxes.Any())
        {
            MessageBox.Show("Please select at least one scope box to copy.", "Information");
            return;
        }

        var linkedDocument = SelectedLinkedModel.LinkInstance.GetLinkDocument();
        if (linkedDocument == null)
        {
            MessageBox.Show("Failed to get linked document. The link might be unloaded.", "Error");
            return;
        }

        using (Transaction transaction = new Transaction(_document, "Copy Scope Boxes"))
        {
            transaction.Start();
            try
            {
                var transform = SelectedLinkedModel.LinkInstance.GetTotalTransform();
                var copiedCount = 0;
                var copiedNames = new List<string>();
                
                using var existingCollector = new FilteredElementCollector(_document);
                var existingScopeBoxNames = existingCollector
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Select(e => e.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);


                foreach (var scopeBoxItem in selectedScopeBoxes)
                {
                    var elementIds = new List<ElementId> { scopeBoxItem.Element.Id };
                    var copiedElementIds = ElementTransformUtils.CopyElements(
                        linkedDocument,
                        elementIds,
                        _document,
                        transform,
                        new CopyPasteOptions());

                    if (copiedElementIds != null && copiedElementIds.Count > 0)
                    {
                        ElementId firstId = copiedElementIds.Cast<ElementId>().FirstOrDefault();
                        if (firstId != null)
                        {
                            var copiedElement = _document.GetElement(firstId);
                            if (copiedElement != null)
                            {
                                string newName = copiedElement.Name;
                                
                                if (existingScopeBoxNames.Contains(scopeBoxItem.Name))
                                {
                                    newName = $"{scopeBoxItem.Name}_{SelectedLinkedModel.Name}";
                                }
                                
                                copiedElement.Name = newName;
                                existingScopeBoxNames.Add(newName);

                                copiedCount++;
                                copiedNames.Add(newName);

                            }
                            else
                            {
                                MessageBox.Show(
                                    $"Failed to retrieve copied element with ID: {firstId}. Skipping name assignment.",
                                    "Warning");
                            }
                        }
                    }
                }

                transaction.Commit();
                var message = $"{copiedCount} Scope Box(es) copied successfully:\n\n";
                message += string.Join("\n", copiedNames.Select(name => $"• {name}"));
            
                MessageBox.Show(message, "Success");

                _parentWindow?.Close();
            }
            catch (Exception e)
            {
                if (transaction.GetStatus() == TransactionStatus.Started)
                {
                    transaction.RollBack();
                }

                MessageBox.Show($"Error copying scope boxes: {e.Message}", "Error");
            }
        }
    }

    private bool CanCopyScopeBoxes()
    {
        return SelectedLinkedModel != null && ScopeBoxes.Any(sb => sb.IsSelected);
    }

    public void UpdateCommandState()
    {
        CopyScopeBoxesCommand?.NotifyCanExecuteChanged();
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Clear collections to help GC
            LinkModels?.Clear();
            ScopeBoxes?.Clear();
            
            _disposed = true;
        }
    }

}
