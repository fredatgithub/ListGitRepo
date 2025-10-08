using System.ComponentModel;

namespace ListGitRepo.Models
{
  public class GitRepository : INotifyPropertyChanged
  {
    private string _name;
    public string Name
    {
      get => _name;
      set
      {
        _name = value;
        OnPropertyChanged(nameof(Name));
      }
    }

    private string _url;
    public string Url
    {
      get => _url;
      set
      {
        _url = value;
        OnPropertyChanged(nameof(Url));
      }
    }

    private string _localPath;
    public string LocalPath
    {
      get => _localPath;
      set
      {
        _localPath = value;
        OnPropertyChanged(nameof(LocalPath));
      }
    }

    private string _branch;
    public string Branch
    {
      get => _branch;
      set
      {
        _branch = value;
        OnPropertyChanged(nameof(Branch));
      }
    }

    private string _lastCommit;
    public string LastCommit
    {
      get => _lastCommit;
      set
      {
        _lastCommit = value;
        OnPropertyChanged(nameof(LastCommit));
      }
    }

    private string _status;
    public string Status
    {
      get => _status;
      set
      {
        _status = value;
        OnPropertyChanged(nameof(Status));
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}