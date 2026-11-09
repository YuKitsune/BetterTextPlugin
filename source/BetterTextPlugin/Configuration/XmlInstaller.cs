using System.IO;

namespace BetterTextPlugin.Configuration;

public static class XmlInstaller
{
    const string BackupSuffix = ".bettertextplugin.bak";
    const string LabelsInstalledMarker = "BTP_LABEL";
    const string StripsInstalledMarker = "BTP_STRIP";

    // The new items are inserted after these anchors.
    // Labels.xml has the anchor in both the Normal and the Extended label.
    const string LabelsAnchor =
        "<Item Type=\"LABEL_ITEM_FIELD18RMK\" Colour=\"\" LeftClick=\"Label_Field18_Popup\" MiddleClick=\"\" RightClick=\"\" />";

    const string LabelsNewItems =
        "      <!-- Better Text Plugin: Voice Capability -->\n" +
        "      <Item Type=\"BTP_LABEL\" />\n" +
        "      <Item Type=\"BTP_LABEL_BG\" BackgroundColour=\"Custom\" />";

    const string StripsAnchor =
        "<StripItem Type=\"FlightRules\" />";

    const string StripsNewItems =
        "                          <!-- Better Text Plugin: Voice Capability -->\n" +
        "                          <StripItem Type=\"BTP_STRIP\" />";

    public static InstallationStatus GetStatus()
    {
        if (!TryFindConfigFiles(out var labels, out var strips, out var error))
            return InstallationStatus.Error(error!);

        var labelsInstalled = File.Exists(labels) && File.ReadAllText(labels).Contains(LabelsInstalledMarker);
        var stripsInstalled = File.Exists(strips) && File.ReadAllText(strips).Contains(StripsInstalledMarker);

        if (labelsInstalled && stripsInstalled)
            return InstallationStatus.Installed();

        return InstallationStatus.NotInstalled();
    }

    public static InstallResult Install()
    {
        if (!TryFindConfigFiles(out var labels, out var strips, out var error))
            return InstallResult.Failed(error!);

        try
        {
            PatchFile(labels, LabelsAnchor, LabelsNewItems);
            PatchFile(strips, StripsAnchor, StripsNewItems);
            return InstallResult.Success();
        }
        catch (InstallException ex)
        {
            return InstallResult.Failed(ex.Message);
        }
    }

    public static InstallResult Uninstall()
    {
        if (!TryFindConfigFiles(out var labels, out var strips, out var error))
            return InstallResult.Failed(error!);

        try
        {
            RestoreFile(labels);
            RestoreFile(strips);
            return InstallResult.Success();
        }
        catch (InstallException ex)
        {
            return InstallResult.Failed(ex.Message);
        }
    }

    static void PatchFile(string filePath, string anchor, string newItems)
    {
        if (!File.Exists(filePath))
            throw new InstallException($"File not found: {filePath}");

        var backupPath = filePath + BackupSuffix;

        // If a backup exists, use it as the base. This prevents a second set of items.
        // If a backup does not exist, make a backup of the current file.
        string content;
        if (File.Exists(backupPath))
        {
            content = File.ReadAllText(backupPath);
        }
        else
        {
            content = File.ReadAllText(filePath);
            File.WriteAllText(backupPath, content);
        }

        if (!content.Contains(anchor))
        {
            throw new InstallException(
                $"Could not find the expected content in {Path.GetFileName(filePath)}. " +
                "The file may have been customised. Please apply the changes manually.");
        }

        // Use the same line endings as the file.
        var newLine = content.Contains("\r\n") ? "\r\n" : "\n";
        var patched = content.Replace(anchor, anchor + newLine + newItems.Replace("\n", newLine));

        File.WriteAllText(filePath, patched);
    }

    static void RestoreFile(string filePath)
    {
        var backupPath = filePath + BackupSuffix;
        if (!File.Exists(backupPath))
        {
            throw new InstallException(
                $"No backup found for {Path.GetFileName(filePath)}. " +
                "The plugin's label & strip items must be removed manually.");
        }

        File.Copy(backupPath, filePath, overwrite: true);
        File.Delete(backupPath);
    }

    static bool TryFindConfigFiles(out string labelsPath, out string stripsPath, out string? error)
    {
        labelsPath = string.Empty;
        stripsPath = string.Empty;
        error = null;

        if (!ProfileDirectoryResolver.TryGetProfileDirectory(out var profileDir) || profileDir is null)
        {
            error = "Profile directory could not be resolved.";
            return false;
        }

        labelsPath = Path.Combine(profileDir.FullName, "Labels.xml");
        stripsPath = Path.Combine(profileDir.FullName, "Strips.xml");
        return true;
    }

    class InstallException(string message) : Exception(message);
}

public readonly struct InstallationStatus
{
    public bool IsInstalled { get; }
    public string? ErrorMessage { get; }

    InstallationStatus(bool isInstalled, string? errorMessage)
    {
        IsInstalled = isInstalled;
        ErrorMessage = errorMessage;
    }

    public static InstallationStatus Installed() => new(true, null);
    public static InstallationStatus NotInstalled() => new(false, null);
    public static InstallationStatus Error(string errorMessage) => new(false, errorMessage);
}

public readonly struct InstallResult
{
    public bool Succeeded { get; }
    public string? ErrorMessage { get; }

    InstallResult(bool succeeded, string? errorMessage)
    {
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    public static InstallResult Success() => new(true, null);
    public static InstallResult Failed(string errorMessage) => new(false, errorMessage);
}
