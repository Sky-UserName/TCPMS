using System.Collections.Concurrent;

namespace TCPMS.Api.Services;

/// <summary>
/// 管理后台登录失败限流。同一账号或同一来源 IP 连续失败达到阈值后锁定一段时间，
/// 防止默认口令/弱口令被在线撞库。进程内实现，多实例部署时需替换为分布式存储。
/// </summary>
public sealed class LoginThrottle
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, Attempt> _attempts = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public bool IsLocked(string key, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        if (!_attempts.TryGetValue(key, out var attempt))
        {
            return false;
        }

        if (attempt.LockedUntil is null)
        {
            return false;
        }

        if (attempt.LockedUntil <= DateTime.UtcNow)
        {
            _attempts.TryRemove(key, out _);
            return false;
        }

        remaining = attempt.LockedUntil.Value - DateTime.UtcNow;
        return true;
    }

    public void RegisterFailure(string key)
    {
        CleanupIfNeeded();
        var now = DateTime.UtcNow;
        _attempts.AddOrUpdate(
            key,
            _ => new Attempt(1, now, null),
            (_, existing) =>
            {
                var withinWindow = now - existing.FirstFailureAt <= FailureWindow;
                var count = withinWindow ? existing.FailureCount + 1 : 1;
                var firstFailureAt = withinWindow ? existing.FirstFailureAt : now;
                var lockedUntil = count >= MaxFailures ? now.Add(LockDuration) : (DateTime?)null;
                return new Attempt(count, firstFailureAt, lockedUntil);
            });
    }

    public void Reset(string key) => _attempts.TryRemove(key, out _);

    private void CleanupIfNeeded()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < CleanupInterval)
        {
            return;
        }

        _lastCleanup = now;
        foreach (var pair in _attempts)
        {
            var expired = pair.Value.LockedUntil is null
                ? now - pair.Value.FirstFailureAt > FailureWindow
                : pair.Value.LockedUntil <= now;
            if (expired)
            {
                _attempts.TryRemove(pair.Key, out _);
            }
        }
    }

    private sealed record Attempt(int FailureCount, DateTime FirstFailureAt, DateTime? LockedUntil);
}
