using System.Configuration;
using System.Windows;

namespace ListGitRepo
{
  public class WindowSettings
  {
    private const string WindowTopKey = "WindowTop";
    private const string WindowLeftKey = "WindowLeft";
    private const string WindowWidthKey = "WindowWidth";
    private const string WindowHeightKey = "WindowHeight";
    private const string WindowStateKey = "WindowState";

    public double Top
    {
      get { return GetValue(WindowTopKey, 100); }
      set { SetValue(WindowTopKey, value); }
    }

    public double Left
    {
      get { return GetValue(WindowLeftKey, 100); }
      set { SetValue(WindowLeftKey, value); }
    }

    public double Width
    {
      get { return GetValue(WindowWidthKey, 1000); }
      set { SetValue(WindowWidthKey, value); }
    }

    public double Height
    {
      get { return GetValue(WindowHeightKey, 600); }
      set { SetValue(WindowHeightKey, value); }
    }

    public WindowState WindowState
    {
      get { return (WindowState)GetValue(WindowStateKey, WindowState.Normal); }
      set { SetValue(WindowStateKey, (int)value); }
    }

    private static T GetValue<T>(string key, T defaultValue)
    {
      try
      {
        var value = ConfigurationManager.AppSettings[key];
        if (value == null)
          return defaultValue;

        return (T)System.Convert.ChangeType(value, typeof(T));
      }
      catch
      {
        return defaultValue;
      }
    }

    private static void SetValue<T>(string key, T value)
    {
      try
      {
        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        if (config.AppSettings.Settings[key] != null)
          config.AppSettings.Settings[key].Value = value?.ToString();
        else
          config.AppSettings.Settings.Add(key, value?.ToString());

        config.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
      }
      catch
      {
        // Ignore errors when saving settings
      }
    }
  }
}