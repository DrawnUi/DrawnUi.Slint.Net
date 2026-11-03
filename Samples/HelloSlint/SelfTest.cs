using DrawnUi.Draw;
using DrawnUi.Slint;
using Slint;

namespace HelloSlint;

/// <summary>Opens pages through the shell, saves each page's frame, prints the frame times and idle redraws, and quits.</summary>
internal static class SelfTest
{
    private static readonly string[] Pages = ["looks", "animations", "shaders", "cells", "text", "shapes", "pong"];

    public static void Run(SlintCanvasHost host, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var times = new List<double>();
        host.FrameRendered += times.Add;
        var shell = HelloApp.Shell;

        var ms = 1500;
        Slint.Timer.SingleShot(TimeSpan.FromMilliseconds(ms), () => host.SaveFrame(Path.Combine(outDir, "0-menu.png")));
        // idle on the root page: a host that draws on demand draws nothing here
        var idleFrom = 0;
        Slint.Timer.SingleShot(TimeSpan.FromMilliseconds(ms += 500), () => idleFrom = times.Count);
        Slint.Timer.SingleShot(TimeSpan.FromMilliseconds(ms += 2000), () => Console.WriteLine($"idle: {times.Count - idleFrom} frames in 2 s"));
        for (var i = 0; i < Pages.Length; i++)
        {
            var (n, route) = (i + 1, Pages[i]);
            Slint.Timer.SingleShot(TimeSpan.FromMilliseconds(ms += 300), () => _ = GoTo(shell, route));
            Slint.Timer.SingleShot(TimeSpan.FromMilliseconds(ms += 1500), () =>
            {
                host.SaveFrame(Path.Combine(outDir, $"{n}-{route}.png"));
                Console.WriteLine($"{route}: {times.Count} frames so far");
            });
        }
        Slint.Timer.SingleShot(TimeSpan.FromMilliseconds(ms += 300), () =>
        {
            var rest = times.Skip(1).ToList();
            Console.WriteLine($"GPU {host.IsGpu}, scale {host.Scale}, frames {times.Count}, average {rest.Average():F2} ms, max {rest.Max():F2} ms");
            EventLoop.Quit();
        });
    }

    private static async Task GoTo(SkiaShell shell, string route)
    {
        await shell.PopToRootAsync();
        await shell.GoToAsync(route, false);
    }
}
