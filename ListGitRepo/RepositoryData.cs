using System;

[Serializable]
public class RepositoryData
{
  public string Name { get; set; }
  public string Url { get; set; }
  public string LocalPath { get; set; }
  public string Status { get; set; }
  public string Branch { get; set; }
  public string LastCommit { get; set; }
}