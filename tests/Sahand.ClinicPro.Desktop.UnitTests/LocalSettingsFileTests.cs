using System.IO;
using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class LocalSettingsFileTests
{
    [Fact]
    public void Group_write_preserves_unrelated_keys_and_replaces_selected_keys()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clinicpro-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(dir, "settings.json");
            LocalSettingsFile.MergeAndWrite(path, new Dictionary<string, string> { ["other"] = "kept", ["copies"] = "1" });
            LocalSettingsFile.MergeAndWrite(path, new Dictionary<string, string> { ["copies"] = "3", ["preview"] = "false" });
            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
            saved.Should().Contain("other", "kept").And.Contain("copies", "3").And.Contain("preview", "false");
            Directory.GetFiles(dir, "*.tmp").Should().BeEmpty();
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void Invalid_existing_file_is_not_silently_destroyed()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clinicpro-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "settings.json");
            File.WriteAllText(path, "{broken");
            Action write = () => LocalSettingsFile.MergeAndWrite(path, new Dictionary<string, string> { ["copies"] = "2" });
            write.Should().Throw<JsonException>();
            File.ReadAllText(path).Should().Be("{broken");
        }
        finally { Directory.Delete(dir, true); }
    }
}
