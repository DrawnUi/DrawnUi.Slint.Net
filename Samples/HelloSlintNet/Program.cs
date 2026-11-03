using System.Globalization;
using DrawnUi.Draw;
using DrawnUi.Models;
using DrawnUi.Slint.Net;
using DrawnUi.Views;
using HelloSlintNet;
using Color = DrawnUi.Color;
using Setter = DrawnUi.Draw.Setter;
using Style = DrawnUi.Draw.Style;

internal static class Program
{
    // Slint windows on Windows need a single-threaded apartment, which top-level statements can't ask for.
    [STAThread]
    private static void Main()
    {
        // Numbers in captions and status lines read the same on every machine and as in the React demo
        // ("0.005", never "0,005" on a comma-decimal locale).
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Startup, same shape as the React demo's main.tsx and the other .NET heads.
        Super.UseDrawnUi()
            .ConfigureFonts(fonts => fonts
                .AddFont("fonts/OpenSans-Regular.ttf", "FontText")
                .AddFont("fonts/OpenSans-Semibold.ttf", "FontText", FontWeight.SemiBold) // FontAttributes=Bold / FontWeight=600 pick this face
                .AddFont("fonts/OpenSans-Semibold.ttf", "FontTextBold")
                .AddFont("fonts/Orbitron-Regular.ttf", "FontGame") // Pong score and messages
                .AddFont("fonts/NotoSansMathSymbols-Subset.ttf", "FontSymbols")
                .AddFont("fonts/NotoSansSymbols2-Subset.ttf", "FontSymbols2")
                // the web heads' Noto emoji subset flattened to COLRv0, which SkiaSharp draws on Windows (DirectWrite) and Linux
                .AddFont("fonts/NotoColorEmoji-Subset-COLRv0.ttf", "FontEmoji"))
            // Without this every control that leaves FontFamily empty draws in Skia's built-in face. A SkiaLabel style does
            // not reach button captions (SkiaButton pushes its own FontFamily onto its label), so style buttons too.
            .ConfigureStyles(styles => styles
                .AddStyle(new Style
                {
                    TargetType = typeof(SkiaLabel),
                    ApplyToDerivedTypes = true,
                    Setters = { new Setter { Property = SkiaLabel.FontFamilyProperty, Value = "FontText" } },
                })
                .AddStyle(new Style
                {
                    TargetType = typeof(SkiaButton),
                    ApplyToDerivedTypes = true,
                    Setters = { new Setter { Property = SkiaButton.FontFamilyProperty, Value = "FontText" } },
                }))
            .Build();

        // Every label is read as text, every button is a button, as in the React demo.
        SkiaLabel.DefaultAccessibilityRole = Aria.RoleText;
        SkiaButton.DefaultAccessibilityRole = Aria.RoleButton;

        // tooltips in the app's popup palette (the menus' panel and edge), so they stand out on the #2B3035 cards
        Super.TooltipStyle = new TooltipStyle
        {
            BackgroundColor = Color.Parse("#262B3B"),
            BorderColor = Color.Parse("#3B4256"),
            TextColor = Colors.White,
        };

        // On the GPU, like the other Hello apps: Slint's FemtoVG renderer on OpenGL, whose context DrawnUI draws on.
        // Shaders and effects run on the CPU otherwise. DRAWNUI_GPU=0 keeps the raster path.
        if (Environment.GetEnvironmentVariable("DRAWNUI_GPU") != "0")
        {
            try
            {
                new Slint.BackendSelector().RendererName("femtovg").RequireOpenGL().Select();
                SlintCanvasHost.UseGpu = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"OpenGL is not available, drawing on the CPU: {e.Message}");
            }
        }

        using var window = MainWindow.Create();
        using var host = SlintCanvasHost.Attach(window.Instance, 0, new Canvas
        {
            BackgroundColor = Color.Parse("#212529"),
            Gestures = GesturesMode.Enabled,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Content = HelloApp.BuildContent(),
        });

        // The Images page photo (also the Shell backdrop and the Scroll header), warmed once the first screen is up, so the
        // first visit to Images shows it at once instead of black tiles.
        EventHandler<SkiaDrawingContext> preload = null;
        preload = (_, _) =>
        {
            host.Canvas.WasDrawn -= preload;
            _ = Task.Run(() => SkiaImageManager.Instance.PreloadImages(new List<string> { "images/baboon.jpg" }));
        };
        host.Canvas.WasDrawn += preload;

        if (Environment.GetEnvironmentVariable("DRAWNUI_SELFTEST") is { Length: > 0 } outDir)
            SelfTest.Run(window, host, outDir);

        window.Run();
    }
}
