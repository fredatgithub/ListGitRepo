using LibGit2Sharp;
using ListGitRepo.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ListGitRepo
{
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private string _status;
    public string Status
    {
      get => _status;
      set
      {
        _status = value;
        OnPropertyChanged(nameof(Status));
        lblStatus.Text = value;
      }
    }

    private ObservableCollection<GitRepository> _repositories;

    public ObservableCollection<GitRepository> Repositories
    {
      get => _repositories;
      set
      {
        _repositories = value;
        OnPropertyChanged(nameof(Repositories));
      }
    }

    private GitRepository _selectedRepository;
    public GitRepository SelectedRepository
    {
      get => _selectedRepository;
      set
      {
        _selectedRepository = value;
        OnPropertyChanged(nameof(SelectedRepository));
        UpdateButtonStates();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public MainWindow()
    {
      InitializeComponent();
      DataContext = this;
      // Charger le chemin de base sauvegardé
    if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.BaseRepositoryPath))
    {
        BaseRepositoryPath = Properties.Settings.Default.BaseRepositoryPath;
    }
    else
    {
        BaseRepositoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Git");
    }
    
    Repositories = new ObservableCollection<GitRepository>();
    LoadRepositories();
    }

    private void UpdateButtonStates()
    {
      bool hasSelection = SelectedRepository != null;
      btnPull.IsEnabled = hasSelection;
      btnOpenFolder.IsEnabled = hasSelection;
      btnOpenInExplorer.IsEnabled = hasSelection;
      btnRemove.IsEnabled = hasSelection;
    }

    private void LoadRepositories()
    {
      try
      {
        // Charger les dépôts depuis un fichier de configuration
        // À implémenter : charger depuis un fichier de configuration
        Repositories.Clear();

        // Exemple de dépôt de test (à supprimer en production)
        // Repositories.Add(new GitRepository { Name = "Test Repo", LocalPath = "C:\\Git\\test-repo" });

        UpdateStatus($"{Repositories.Count} dépôts chargés");
        UpdateLastUpdateTime();
      }
      catch (Exception exception)
      {
        ShowError("Erreur lors du chargement des dépôts", exception);
      }
    }

    private void BtnAddRepo_Click(object sender, RoutedEventArgs e)
{
    string repoUrl = txtRepoUrl.Text.Trim();
    if (string.IsNullOrEmpty(repoUrl))
    {
        MessageBox.Show("Veuillez entrer une URL de dépôt valide.", "Erreur", 
                      MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    try
    {
        if (Repositories.Any(r => r.Url.Equals(repoUrl, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Ce dépôt est déjà dans la liste.", "Information", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var repoName = Path.GetFileNameWithoutExtension(repoUrl);
        var repo = new GitRepository
        {
            Name = repoName,
            Url = repoUrl,
            LocalPath = Path.Combine(BaseRepositoryPath, repoName),
            Status = "Non cloné"
        };

        Repositories.Add(repo);
        SaveRepositories();
        txtRepoUrl.Clear();
        UpdateStatus($"Dépôt ajouté : {repo.Name}");
    }
    catch (Exception exception)
    {
        ShowError("Erreur lors de l'ajout du dépôt", exception);
    }
}

    private async void BtnCloneRepo_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(txtRepoUrl.Text))
      {
        MessageBox.Show("Veuillez d'abord ajouter un dépôt avec une URL valide.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      await Task.Run(() =>
      {
        try
        {
          string repoUrl = txtRepoUrl.Text.Trim();
          var repo = Repositories.FirstOrDefault(r => r.Url.Equals(repoUrl, StringComparison.OrdinalIgnoreCase));

          if (repo == null)
          {
            BtnAddRepo_Click(sender, e);
            repo = Repositories.Last();
          }

          Dispatcher.Invoke(() =>
                {
                  Status = "Clonage en cours...";
                  repo.Status = "Clonage...";
                });

          // Créer le répertoire parent si nécessaire
          Directory.CreateDirectory(Path.GetDirectoryName(repo.LocalPath));

          // Cloner le dépôt
          Repository.Clone(repo.Url, repo.LocalPath);

          Dispatcher.Invoke(() =>
                {
                  repo.Status = "À jour";
                  UpdateStatus($"Dépôt cloné avec succès : {repo.Name}");
                  SaveRepositories();
                });
        }
        catch (Exception exception)
        {
          Dispatcher.Invoke(() =>
                {
                  ShowError("Erreur lors du clonage du dépôt", exception);
                  var repo = Repositories.FirstOrDefault(r => r.Url.Equals(txtRepoUrl.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                  if (repo != null)
                  {
                    repo.Status = "Erreur";
                  }
                });
        }
      });
    }

    private async void BtnPull_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedRepository == null) return;

      await Task.Run(() =>
      {
        try
        {
          Dispatcher.Invoke(() =>
                {
                  Status = "Mise à jour en cours...";
                  SelectedRepository.Status = "Mise à jour...";
                });

          using (var repo = new Repository(SelectedRepository.LocalPath))
          {
            // Récupérer les modifications distantes
            var options = new PullOptions
            {
              FetchOptions = new FetchOptions()
            };

            // Exécuter le pull
            var signature = new Signature(new Identity("Git Manager", "git@manager.com"), DateTimeOffset.Now);
            var result = Commands.Pull(repo, signature, options);

            Dispatcher.Invoke(() =>
                  {
                    if (result.Status == MergeStatus.UpToDate)
                    {
                      SelectedRepository.Status = "À jour";
                      UpdateStatus($"Le dépôt {SelectedRepository.Name} est déjà à jour.");
                    }
                    else
                    {
                      SelectedRepository.Status = "À jour";
                      UpdateStatus($"Mise à jour réussie pour {SelectedRepository.Name}. {result.Commit?.MessageShort}");
                    }

                    // Mettre à jour les informations du dépôt
                    SelectedRepository.Branch = repo.Head.FriendlyName;
                    SelectedRepository.LastCommit = repo.Head.Tip?.MessageShort ?? "Aucun commit";
                    SaveRepositories();
                  });
          }
        }
        catch (Exception exception)
        {
          Dispatcher.Invoke(() =>
                {
                  ShowError($"Erreur lors de la mise à jour du dépôt {SelectedRepository.Name}", exception);
                  SelectedRepository.Status = "Erreur";
                });
        }
      });
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedRepository == null) return;

      try
      {
        if (Directory.Exists(SelectedRepository.LocalPath))
        {
          Process.Start("explorer.exe", SelectedRepository.LocalPath);
          UpdateStatus($"Ouverture du dossier : {SelectedRepository.LocalPath}");
        }
        else
        {
          MessageBox.Show("Le dossier du dépôt n'existe pas encore. Veuillez d'abord cloner le dépôt.",
              "Dossier introuvable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      }
      catch (Exception exception)
      {
        ShowError("Erreur lors de l'ouverture du dossier", exception);
      }
    }

    private void BtnOpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
      BtnOpenFolder_Click(sender, e);
    }

    private void BtnRemove_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedRepository == null) return;

      var result = MessageBox.Show(
          $"Voulez-vous vraiment supprimer le dépôt '{SelectedRepository.Name}' de la liste ?\n\n" +
          "Note : Cette action ne supprime pas les fichiers locaux du dépôt.",
          "Confirmer la suppression",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);

      if (result == MessageBoxResult.Yes)
      {
        var repoToRemove = SelectedRepository;
        Repositories.Remove(repoToRemove);
        SaveRepositories();
        UpdateStatus($"Dépôt supprimé : {repoToRemove.Name}");
      }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
      LoadRepositories();
      UpdateStatus("Liste des dépôts actualisée");
    }

    private void SaveRepositories()
    {
      try
      {
        // À implémenter : sauvegarder dans un fichier de configuration
        // Par exemple : JSON ou XML
        UpdateLastUpdateTime();
      }
      catch (Exception exception)
      {
        ShowError("Erreur lors de la sauvegarde des dépôts", exception);
      }
    }

    private void UpdateStatus(string message)
    {
      Status = message;
      Debug.WriteLine($"[STATUS] {message}");
    }

    private void UpdateLastUpdateTime()
    {
      lblLastUpdate.Text = $"Dernière mise à jour : {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
    }

    private void ShowError(string message, Exception exception)
    {
      string errorMessage = $"{message}: {exception.Message}";
      Debug.WriteLine($"[ERREUR] {errorMessage}");
      Debug.WriteLine(exception.StackTrace);

      Dispatcher.Invoke(() =>
      {
        MessageBox.Show(errorMessage, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        Status = "Erreur : " + exception.Message;
      });
    }

    protected virtual void OnPropertyChanged(string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      if (WindowState == WindowState.Maximized)
      {
        // Utiliser les propriétés RestoreBounds pour obtenir la taille et la position avant la maximisation
        var settings = (WindowSettings)FindResource("WindowSettings");
        settings.Top = RestoreBounds.Top;
        settings.Left = RestoreBounds.Left;
        settings.Width = RestoreBounds.Width;
        settings.Height = RestoreBounds.Height;
        settings.WindowState = WindowState.Maximized;
      }
      else
      {
        // Sauvegarder la taille et la position normales
        var settings = (WindowSettings)FindResource("WindowSettings");
        settings.Top = Top;
        settings.Left = Left;
        settings.Width = Width;
        settings.Height = Height;
        settings.WindowState = WindowState.Normal;
      }
    }

    private string BaseRepositoryPath
{
    get
    {
        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Git");
        return !string.IsNullOrWhiteSpace(txtBaseDirectory.Text) 
            ? txtBaseDirectory.Text 
            : defaultPath;
    }
    set => txtBaseDirectory.Text = value;
}

private void TxtBaseDirectory_TextChanged(object sender, TextChangedEventArgs e)
{
    // Sauvegarder le chemin dans les paramètres
    Properties.Settings.Default.BaseRepositoryPath = txtBaseDirectory.Text;
    Properties.Settings.Default.Save();
}

private void BtnBrowse_Click(object sender, RoutedEventArgs e)
{
    var dialog = new System.Windows.Forms.FolderBrowserDialog
    {
        SelectedPath = Directory.Exists(txtBaseDirectory.Text) 
            ? txtBaseDirectory.Text 
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
    };

    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
        BaseRepositoryPath = dialog.SelectedPath;
    }
}
  }
}