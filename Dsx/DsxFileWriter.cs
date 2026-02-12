namespace Rf2DsxBridge.Dsx;

public sealed class DsxFileWriter
{
    private readonly string _filePath;
    private readonly string _tempPath;
    private bool _pathValid;

    public DsxFileWriter(string filePath)
    {
        _filePath = filePath;
        _tempPath = filePath + ".tmp";
        ValidatePath();
    }

    private void ValidatePath()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _pathValid = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DsxFileWriter] Invalid path '{_filePath}': {ex.Message}");
            _pathValid = false;
        }
    }

    public bool Write(TriggerOutput output)
    {
        if (!_pathValid)
            return false;

        try
        {
            string content = BuildContent(output);
            File.WriteAllText(_tempPath, content);
            File.Move(_tempPath, _filePath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DsxFileWriter] Write error: {ex.Message}");
            return false;
        }
    }

    public bool WriteNormal()
    {
        var output = new TriggerOutput
        {
            LeftMode = "Normal",
            LeftForceA = 0,
            LeftForceB = 0,
            RightMode = "Normal",
            RightForceA = 0,
            RightForceB = 0
        };
        return Write(output);
    }

    private static string BuildContent(TriggerOutput output)
    {
        if (output.LeftMode == "Normal" && output.RightMode == "Normal")
        {
            return "LeftTrigger=Normal\nRightTrigger=Normal";
        }

        var sb = new System.Text.StringBuilder(128);
        sb.Append("LeftTrigger=").AppendLine(output.LeftMode);

        if (output.LeftMode != "Normal")
        {
            sb.Append("ForceLeftTrigger=(")
              .Append(output.LeftForceA).Append(")(")
              .Append(output.LeftForceB).AppendLine(")");
        }

        sb.Append("RightTrigger=").AppendLine(output.RightMode);

        if (output.RightMode != "Normal")
        {
            sb.Append("ForceRightTrigger=(")
              .Append(output.RightForceA).Append(")(")
              .Append(output.RightForceB).Append(")");
        }

        return sb.ToString();
    }
}
