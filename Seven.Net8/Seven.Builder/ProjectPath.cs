namespace Seven.Builder;

/// <summary>解析代码生成输出路径</summary>
public static class ProjectPath
{
    /// <summary>Seven.Master 根目录</summary>
    public static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Seven.Net8")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Seven.Vue3")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    public static string DomainPath => Path.Combine(GetSolutionRoot(), "Seven.Net8", "Seven.Domain", "Entities");
    public static string ApplicationPath => Path.Combine(GetSolutionRoot(), "Seven.Net8", "Seven.Application", "Interfaces");
    public static string InfrastructurePath => Path.Combine(GetSolutionRoot(), "Seven.Net8", "Seven.Infrastructure", "Services");
    public static string WebApiPath => Path.Combine(GetSolutionRoot(), "Seven.Net8", "Seven.WebApi", "Controllers");
    public static string VueViewsPath => Path.Combine(GetSolutionRoot(), "Seven.Vue3", "src", "views", "system");
    public static string TemplatePath => Path.Combine(GetSolutionRoot(), "Seven.Net8", "Seven.WebApi", "Template");

    public static string[] GetNamespaces() =>
    [
        "Seven.Domain.Entities.System",
        "Seven.Domain.Entities.Core",
        "Seven.Domain.Entities.Alarm",
        "Seven.Domain.Entities.Board",
    ];
}

/// <summary>文件读写辅助</summary>
public static class FileHelper
{
    public static string ReadTemplate(string name)
    {
        var path = Path.Combine(ProjectPath.TemplatePath, name);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public static void WriteFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
    }

    public static string ReplaceTokens(string template, Dictionary<string, string> tokens)
    {
        var result = template;
        foreach (var (key, value) in tokens)
            result = result.Replace("{{" + key + "}}", value);
        return result;
    }
}
