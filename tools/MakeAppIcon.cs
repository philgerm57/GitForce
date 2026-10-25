// Generates the GitForce application icon (src/git.ico and src/Resources/App/git.ico):
//   dotnet run tools/MakeAppIcon.cs -- <output folder>
// writes gitforce.ico and preview.png in that folder.
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true
#:property PublishAot=false
#:property PublishTrimmed=false
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class Make {
  // The icon on a 24 x 24 grid: a rounded square with a blue to violet gradient and a white git branch
  static Bitmap Draw(int size) {
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp)) {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.PixelOffsetMode = PixelOffsetMode.HighQuality;
      float k = size / 24f;
      float m = size <= 20 ? 0.5f : size * 0.03f;            // Margin around the square
      float r = size * 0.23f;                                 // Corner radius
      var rect = new RectangleF(m, m, size - 2 * m, size - 2 * m);
      using (var path = Rounded(rect, r))
      using (var brush = new LinearGradientBrush(new PointF(0, 0), new PointF(size, size), Color.FromArgb(37, 99, 235), Color.FromArgb(124, 58, 237))) {
        g.FillPath(brush, path);
        if (size >= 32) {
          // A soft light on the top half gives the square some depth
          using (var light = new LinearGradientBrush(new PointF(0, -1), new PointF(0, size + 1), Color.FromArgb(52, 255, 255, 255), Color.FromArgb(0, 255, 255, 255)))
            g.FillPath(light, path);
        }
      }
      g.ScaleTransform(k, k);
      // Strokes a bit heavier in the small sizes so that the glyph stays readable
      float w = size <= 16 ? 2.9f : size <= 24 ? 2.6f : size <= 48 ? 2.3f : 2.1f;
      float nr = size <= 16 ? 2.6f : 2.35f;                   // Radius of the commit dots
      using (var pen = new Pen(Color.White, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) {
        g.DrawLine(pen, 8.5f, 6.5f, 8.5f, 17.5f);                         // The main line
        g.DrawBezier(pen, 15.5f, 9f, 15.5f, 13.5f, 12.5f, 15.2f, 8.6f, 15.6f); // The branch joining it
      }
      foreach (var c in new[] { new PointF(8.5f, 6.5f), new PointF(8.5f, 17.5f), new PointF(15.5f, 8.5f) })
        g.FillEllipse(Brushes.White, c.X - nr, c.Y - nr, 2 * nr, 2 * nr);
      // The commit of the branch in a warm color: the "force" of the name
      float ar = nr * 0.55f;
      using (var accent = new SolidBrush(Color.FromArgb(251, 191, 36)))
        g.FillEllipse(accent, 15.5f - ar, 8.5f - ar, 2 * ar, 2 * ar);
    }
    return bmp;
  }

  static GraphicsPath Rounded(RectangleF r, float rad) {
    var p = new GraphicsPath(); float d = 2 * rad;
    p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
    p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
    p.CloseFigure(); return p;
  }

  // An .ico file: 32-bit DIB images up to 64 pixels, a PNG image for 256 pixels
  static void WriteIco(string file, int[] sizes) {
    var images = new List<byte[]>();
    foreach (int s in sizes) {
      using (var bmp = Draw(s)) {
        if (s >= 256) { using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); images.Add(ms.ToArray()); } continue; }
        using (var ms = new MemoryStream()) using (var bw = new BinaryWriter(ms)) {
          bw.Write(40); bw.Write(s); bw.Write(s * 2); bw.Write((short)1); bw.Write((short)32);
          bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
          for (int y = s - 1; y >= 0; y--)
            for (int x = 0; x < s; x++) { Color c = bmp.GetPixel(x, y); bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A); }
          int maskRow = ((s + 31) / 32) * 4;
          bw.Write(new byte[maskRow * s]);
          images.Add(ms.ToArray());
        }
      }
    }
    using (var fs = File.Create(file)) using (var bw = new BinaryWriter(fs)) {
      bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
      int offset = 6 + 16 * sizes.Length;
      for (int i = 0; i < sizes.Length; i++) {
        int s = sizes[i];
        bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((short)1); bw.Write((short)32); bw.Write(images[i].Length); bw.Write(offset);
        offset += images[i].Length;
      }
      foreach (var img in images) bw.Write(img);
    }
  }

  static void Main(string[] a) {
    string dir = a[0];
    int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
    WriteIco(Path.Combine(dir, "gitforce.ico"), sizes);
    // Preview: every size on a light and a dark background, and the big one
    var prev = new Bitmap(700, 330);
    using (var g = Graphics.FromImage(prev)) {
      g.Clear(Color.White);
      using (var dark = new SolidBrush(Color.FromArgb(32, 32, 35))) g.FillRectangle(dark, 350, 0, 350, 330);
      for (int half = 0; half < 2; half++) {
        int x0 = half * 350 + 15;
        g.DrawImage(Draw(256), x0 + 40, 10, 256, 256);
        int x = x0;
        foreach (int s in new[] { 16, 20, 24, 32, 40, 48 }) { g.DrawImage(Draw(s), x, 300 - s / 2, s, s); x += s + 10; }
      }
    }
    prev.Save(Path.Combine(dir, "preview.png"));
    // Check that the .ico loads like Windows and .NET load it
    using (var icon = new Icon(Path.Combine(dir, "gitforce.ico"), 32, 32)) Console.WriteLine("Icon OK: " + icon.Width + "x" + icon.Height);
    using (var icon = new Icon(Path.Combine(dir, "gitforce.ico"), 256, 256)) Console.WriteLine("Icon OK: " + icon.Width + "x" + icon.Height + "  (" + new FileInfo(Path.Combine(dir, "gitforce.ico")).Length / 1024 + " Ko)");
  }
}
