namespace TCPMS.Api.Infrastructure;

/// <summary>
/// 统一解析静态文件根目录与上传目录。
/// 开发环境从 bin 目录直接启动时，静态文件仍然指向项目 wwwroot，
/// 避免上传文件落到 bin/wwwroot、切换启动目录后图片 404。
/// </summary>
public static class StoragePaths
{
    public static string WebRoot(IWebHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            return environment.WebRootPath;
        }

        return Path.Combine(environment.ContentRootPath, "wwwroot");
    }

    public static string Uploads(IWebHostEnvironment environment, params string[] segments)
    {
        var parts = new List<string> { WebRoot(environment), "uploads" };
        parts.AddRange(segments);
        return Path.Combine(parts.ToArray());
    }

    /// <summary>
    /// 从运行目录向上查找 TCPMS.Api.csproj，定位开发项目目录。
    /// 发布目录没有 csproj，返回 null，继续使用默认的 wwwroot。
    /// </summary>
    public static string? FindProjectDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "TCPMS.Api.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
