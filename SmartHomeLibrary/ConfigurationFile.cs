using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
  public class ConfigurationFile
  {
    List<string> lines = new List<string>();
    Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>();

    public void ClearConfiguration()
    {
      lines.Clear();
      sections.Clear();
    }

    /// return error string
    public string ReadConfigurationFromFile(string filename, bool createFileIfNotExists)
    {
      ClearConfiguration();
      if (!File.Exists(filename))
      {
        if (createFileIfNotExists)
          File.WriteAllText(filename, "");
        else
          return "File '" + filename + "' not exists.";
      }

      lines = new List<string>();
      try
      {
        string[] lines_ = File.ReadAllLines(filename, Encoding.UTF8);
        lines.AddRange(lines_);
      }
      catch
      {
        return "Can't load file '" + filename + "'.";
      }

      try
      {
        string currectSection = "";
        sections.Add(currectSection, new Dictionary<string, string>());
        foreach (string line in lines)
        {
          string line_ = line.Trim();
          if (line_.Length > 0)
          {
            bool comment = line_[0] == ';' || line_[0] == '#';
            bool header = line_[0] == '[' && line_[line_.Length - 1] == ']';
            if (header)
            {
              string section = line_.Substring(1, line_.Length - 2).Trim();
              if (sections.ContainsKey(section))
              {
                ClearConfiguration();
                return "Duplicated section '" + section + "'";
              }
              currectSection = section.ToLower();
              sections.Add(currectSection, new Dictionary<string, string>());
            }
            else if (!comment)
            {
              string key;
              string value = "";
              int ix = line_.IndexOf('=');
              if (ix >= 0)
              {
                if (ix == 0)
                  return "Invalid line: " + line;
                key = line_.Substring(0, ix).Trim();
                value = line_.Substring(ix + 1, line_.Length - ix - 1).Trim();
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                  value = value.Substring(1, value.Length - 2);
              }
              else
              {
                ClearConfiguration();
                return "Invalid line: " + line;
              }
              if (sections[currectSection].ContainsKey(key.ToLower()))
              {
                ClearConfiguration();
                return "Duplicate key '" + key + "'";
              }
              sections[currectSection].Add(key.ToLower(), value);
            }
          }
        }
        return "";
      }
      catch
      {
        ClearConfiguration();
        return "Can't parse configuration file.";
      }
    }

    public bool WriteConfigurationToFile(string filename)
    {
      if (!File.Exists(filename))
        return false;

      try
      {
        StringBuilder sb = new StringBuilder();
        foreach (string line in lines)
          sb.AppendLine(line);
        File.WriteAllText(filename, sb.ToString(), Encoding.UTF8);
        return true;
      }
      catch
      {
        return false;
      }
    }

    public bool IsSection(string section)
    {
      return sections.ContainsKey(section.ToLower());
    }

    public bool IsKey(string section, string name)
    {
      if (!IsSection(section))
        return false;
      return sections[section.ToLower()].ContainsKey(name.ToLower());
    }

    public void DeleteSection(string section)
    {
      if (!IsSection(section))
        return;
      string sectionLower = section.ToLower();
      sections.Remove(sectionLower);

      lock (lines)
      {
        string currectSection = "";
        List<string> newLines = new List<string>();
        foreach (string line in lines)
        {
          string line_ = line.Trim();
          if (line_.Length > 0)
          {
            bool comment = line_[0] == ';' || line_[0] == '#';
            bool header = line_[0] == '[' && line_[line_.Length - 1] == ']';
            if (header)
              currectSection = line_.Substring(1, line_.Length - 2).Trim().ToLower();
          }
          if (currectSection != sectionLower)
            newLines.Add(line);
        }
        lines = newLines;
      }
    }

    public void DeleteKey(string section, string key)
    {
      if (!IsSection(section))
        return;
      string sectionLower = section.ToLower();
      string keyLower = key.ToLower();
      sections[sectionLower].Remove(keyLower);

      lock (lines)
      {
        string currectSection = "";
        List<string> newLines = new List<string>();
        foreach (string line in lines)
        {
          string line_ = line.Trim();
          string key_ = "";
          if (line_.Length > 0)
          {
            bool comment = line_[0] == ';' || line_[0] == '#';
            bool header = line_[0] == '[' && line_[line_.Length - 1] == ']';
            if (header)
              currectSection = line_.Substring(1, line_.Length - 2).Trim().ToLower();
            else if (!comment)
            {
              int ix = line_.IndexOf('=');
              if (ix > 0)
                key_ = line_.Substring(0, ix).Trim().ToLower();
            }
          }
          if (currectSection != sectionLower || key_ != keyLower)
            newLines.Add(line);
        }
        lines = newLines;
      }
    }

    public void AddSection(string section)
    {
      sections.Add(section.ToLower(), new Dictionary<string, string>());
    }

    public void SetValue(string section, string key, string value)
    {
      if (!IsSection(section))
        AddSection(section);
      string sectionLower = section.ToLower();
      string keyLower = key.ToLower();
      if (!IsKey(section, key))
        sections[sectionLower].Add(keyLower, value);
      else
        sections[sectionLower][keyLower] = value;

      lock (lines)
      {
        bool sectionFound = false;
        bool changed = false;
        string currectSection = "";
        List<string> newLines = new List<string>();
        foreach (string line in lines)
        {
          string line_ = line.Trim();
          string key_ = "";
          if (line_.Length > 0)
          {
            bool comment = line_[0] == ';' || line_[0] == '#';
            bool header = line_[0] == '[' && line_[line_.Length - 1] == ']';
            if (header)
            {
              if (!changed && currectSection == sectionLower)
              {
                while (newLines.Count > 0 && newLines[newLines.Count - 1] == "")
                  newLines.RemoveAt(newLines.Count - 1);
                newLines.Add(key + " = " + value);
                newLines.Add("");
                changed = true;
              }
              currectSection = line_.Substring(1, line_.Length - 2).Trim().ToLower();
              if (currectSection == sectionLower)
                sectionFound = true;
            }
            else if (!comment)
            {
              int ix = line_.IndexOf('=');
              if (ix > 0)
                key_ = line_.Substring(0, ix).Trim();
            }
          }
          if (currectSection == sectionLower && key_.ToLower() == keyLower)
          {
            newLines.Add(key_ + " = " + value);
            changed = true;
          }
          else
            newLines.Add(line);
        }

        if (!changed && currectSection == sectionLower)
        {
          while (newLines.Count > 0 && newLines[newLines.Count - 1] == "")
            newLines.RemoveAt(newLines.Count - 1);
          newLines.Add(key + " = " + value);
          newLines.Add("");
          changed = true;
        }
        if (!changed && !sectionFound)
        {
          newLines.Add("");
          newLines.Add("[" + section + "]");
          newLines.Add(key + " = " + value);
        }

        lines = newLines;
      }
    }

    public bool ReadString(string section, string key, out string value)
    {
      value = "";
      if (!IsSection(section))
        return false;
      if (!IsKey(section, key))
        return false;
      value = sections[section.ToLower()][key.ToLower()];
      return true;
    }

    public string ReadString(string section, string key, string defaultValue)
    {
      if (!IsSection(section))
        return defaultValue;
      if (!IsKey(section, key))
        return defaultValue;
      return sections[section.ToLower()][key.ToLower()];
    }

    public bool ReadBool(string section, string key, out bool value)
    {
      value = false;
      if (!IsSection(section))
        return false;
      if (!IsKey(section, key))
        return false;
      string valueS = sections[section.ToLower()][key.ToLower()].ToLower();
      if (valueS == "true" || valueS == "1")
      {
        value = true;
        return true;
      }
      if (valueS == "false" || valueS == "0")
      {
        value = false;
        return true;
      }
      return false;
    }

    public bool ReadBool(string section, string key, bool defaultValue)
    {
      if (!IsSection(section))
        return defaultValue;
      if (!IsKey(section, key))
        return defaultValue;
      string valueS = sections[section.ToLower()][key.ToLower()].ToLower();
      if (valueS == "true" || valueS == "1")
        return true;
      if (valueS == "false" || valueS == "0")
        return false;
      return defaultValue;
    }

    public bool ReadInt(string section, string key, out int value)
    {
      value = 0;
      string valueS;
      if (!ReadString(section, key, out valueS))
        return false;
      int a;
      if (!int.TryParse(valueS, out a))
        return false;
      value = a;
      return true;
    }

    public int ReadInt(string section, string key, int defaultValue)
    {
      string valueS;
      if (!ReadString(section, key, out valueS))
        return defaultValue;
      int a;
      if (!int.TryParse(valueS, out a))
        return defaultValue;
      return a;
    }

    public bool ReadFloat(string section, string key, out float value)
    {
      value = 0;
      string valueS;
      if (!ReadString(section, key, out valueS))
        return false;
      float a;
      if (!float.TryParse(valueS, out a))
        return false;
      value = a;
      return true;
    }

    public float ReadFloat(string section, string key, float defaultValue)
    {
      string valueS;
      if (!ReadString(section, key, out valueS))
        return defaultValue;
      float a;
      if (!float.TryParse(valueS, out a))
        return defaultValue;
      return a;
    }
  }
}
