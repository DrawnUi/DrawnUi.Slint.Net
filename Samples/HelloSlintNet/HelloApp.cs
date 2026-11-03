using System.Reflection;
using DrawnUi.Draw;
using DrawnUi.Slint.Net;
using DrawnUi.Views;
using HelloSlintNet.Pages;
using SkiaSharp;
using Color = DrawnUi.Color;
using Thickness = DrawnUi.Views.Thickness;

namespace HelloSlintNet;

/// <summary>
/// The drawn demo: a <see cref="SkiaShell"/> with the root menu underneath and the sample pages pushed on top,
/// wired as the other .NET Hello apps and the React demo's main.tsx do.
/// </summary>
public static class HelloApp
{
    /// <summary>The app's shell, for the self-test.</summary>
    public static SkiaShell Shell { get; private set; }

    /// <summary>Builds the canvas content.</summary>
    public static SkiaControl BuildContent()
    {
        var shell = Shell = new SkiaShell();
        // The Shapes and Shell pages need the shell itself, so the routes are attached after construction.
        shell.Routes = new Dictionary<string, Func<ShellArguments, SkiaControl>>
        {
            ["cells"] = _ => new CellsPage(),
            ["uneven"] = _ => new UnevenCellsPage(),
            ["images"] = _ => new ImagesPage(),
            ["shapes"] = _ => new ShapesPage(shell),
            ["svg"] = _ => new SvgPage(),
            ["text"] = _ => new TextPage(),
            ["layouts"] = _ => new LayoutsPage(),
            ["looks"] = _ => new LooksPage(),
            ["snapping"] = _ => new SnappingPage(),
            ["animations"] = _ => new AnimationsPage(),
            ["shell"] = _ => new ShellPage(shell),
            ["menus"] = _ => new TooltipsMenusPage(),
            ["editor"] = _ => new EditorPage(),
            ["keyboard"] = _ => new KeyboardPage(),
            ["scroll"] = _ => new ScrollPage(),
            ["shaders"] = _ => new ShadersPage(),
            ["sprites"] = _ => new SpritesPage(),
            ["transforms"] = _ => new TransformsPage(),
            ["reorder"] = _ => new ReorderPage(),
            ["pong"] = _ => new PongPage(),
            ["a11y"] = _ => new AccessibilityPage(),
        };
        shell.Titles = Catalog.Samples.ToDictionary(s => s.Route, s => s.Title);

        // A tap on a page's empty space (or a card's) takes the focus there, so a focused editor lets go of its caret;
        // buttons keep it. The shell's page layer blocks taps below without taking the focus.
        foreach (var route in shell.Routes.Keys.ToList())
        {
            var makePage = shell.Routes[route];
            shell.Routes[route] = arguments =>
            {
                var page = makePage(arguments);
                page.CanBeFocused = true;
                page.BlockGesturesBelow = true;
                return page;
            };
        }

        shell.RootContent = new RootPage
        {
            SampleSelected = sample => _ = shell.GoToAsync(sample.Route),
        };

        return new SkiaLayer
        {
            VerticalOptions = LayoutOptions.Fill,
            Children = new List<SkiaControl>
            {
                shell,
#if DEBUG
                // debug builds only, like the React demo's dev-server counter
                new SkiaLabelFps
                {
                    Margin = new Thickness(0, 0, 4, 24),
                    VerticalOptions = LayoutOptions.End,
                    HorizontalOptions = LayoutOptions.End,
                    Rotation = -45,
                    BackgroundColor = Color.Parse("#8B0000"),
                    TextColor = Colors.White,
                    ZIndex = 110,
                },
#endif
            },
        }
        // a right click (long press, Menu key) that no control took shows the library versions
        .OnContextMenu((me, e) =>
        {
            shell.ShowToast($"DrawnUi.Slint {VersionOf(typeof(SlintCanvasHost))} · DrawnUi.Net {VersionOf(typeof(SkiaControl))} · SkiaSharp {VersionOf(typeof(SKCanvas))}", 3000);
            return true;
        });
    }

    private static string VersionOf(Type type)
    {
        var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? type.Assembly.GetName().Version?.ToString() ?? "?";
        var plus = version.IndexOf('+');
        return plus > 0 ? version[..plus] : version;
    }
}
