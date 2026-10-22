using System.Drawing.Drawing2D;
using RdpKeySwap.Interop;

namespace RdpKeySwap.Ui;

/// <summary>Draws the tray icon at runtime: two crossing arrows in a rounded square.</summary>
internal sealed class AppIcon : IDisposable
{
    private readonly IntPtr _handle;

    private AppIcon(Icon icon, IntPtr handle)
    {
        Icon = icon;
        _handle = handle;
    }

    public Icon Icon { get; }

    public static AppIcon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var shape = new GraphicsPath();
            shape.AddArc(1, 1, 12, 12, 180, 90);
            shape.AddArc(19, 1, 12, 12, 270, 90);
            shape.AddArc(19, 19, 12, 12, 0, 90);
            shape.AddArc(1, 19, 12, 12, 90, 90);
            shape.CloseFigure();

            using var fill = new SolidBrush(Color.FromArgb(255, 30, 78, 160));
            graphics.FillPath(fill, shape);

            using var pen = new Pen(Color.White, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            graphics.DrawLine(pen, 9f, 12f, 23f, 12f);
            graphics.DrawLine(pen, 23f, 12f, 19f, 8f);
            graphics.DrawLine(pen, 23f, 12f, 19f, 16f);
            graphics.DrawLine(pen, 23f, 21f, 9f, 21f);
            graphics.DrawLine(pen, 9f, 21f, 13f, 17f);
            graphics.DrawLine(pen, 9f, 21f, 13f, 25f);
        }

        var handle = bitmap.GetHicon();
        return new AppIcon(Icon.FromHandle(handle), handle);
    }

    public void Dispose()
    {
        Icon.Dispose();
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_handle);
        }
    }
}
