using CommunityToolkit.Mvvm.ComponentModel;

namespace HashcatGUI.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}
