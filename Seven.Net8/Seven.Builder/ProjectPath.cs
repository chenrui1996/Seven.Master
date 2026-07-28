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
    public static string VueSrcPath => Path.Combine(GetSolutionRoot(), "Seven.Vue3", "src");
    public static string VueViewsPath => Path.Combine(VueSrcPath, "views");
    public static string VueExtensionPath => Path.Combine(VueSrcPath, "extension");
    public static string VueLocalesPath => Path.Combine(VueSrcPath, "locales", "lang");
    public static string TemplatePath => Path.Combine(GetSolutionRoot(), "Seven.Net8", "Seven.WebApi", "Template");

    /// <summary>视图文件夹默认取命名空间最底层（如 Seven.Domain.Entities.Board → Board）</summary>
    public static string ResolveVueFolder(string? folderName, string? nameSpace)
    {
        if (!string.IsNullOrWhiteSpace(folderName))
            return folderName.Trim();
        if (string.IsNullOrWhiteSpace(nameSpace))
            return "system";
        var last = nameSpace.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrWhiteSpace(last) ? "system" : last;
    }

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
            result = result.Replace("__" + key + "__", value);
        foreach (var (key, value) in tokens)
            result = result.Replace("{{" + key + "}}", value);

        // Vue script 枚举 options：注释占位，未提供时清空，避免残留或 {{EnumOptions}} 运行时报错
        var enumBlock = tokens.TryGetValue("EnumOptions", out var eo) ? eo ?? "" : "";
        result = result
            .Replace("/*__ENUM_OPTIONS__*/", enumBlock)
            .Replace("{{EnumOptions}}", enumBlock)
            .Replace("__EnumOptions__", enumBlock);
        return result;
    }
}
