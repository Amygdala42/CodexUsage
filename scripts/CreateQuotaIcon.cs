using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class CreateQuotaIcon
{
    public static void Main(string[] args)
    {
        var frames = new List<byte[]>();
        int[] sizes = { 16, 32, 48, 256 };
        const int supersampling = 4;
        foreach (int size in sizes)
        using (var source = new Bitmap(size * supersampling, size * supersampling, PixelFormat.Format32bppPArgb))
        using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(source))
        using (var resized = Graphics.FromImage(bitmap))
        using (var attributes = new ImageAttributes())
        using (var memory = new MemoryStream())
        {
            float scale = size * supersampling / 32f;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.Clear(Color.Transparent);
            using (var fill = new SolidBrush(Color.FromArgb(24, 31, 39)))
                graphics.FillEllipse(fill, .75f * scale, .75f * scale, 30.5f * scale, 30.5f * scale);
            // Two solid progress discs match the two-row quota/time widget.
            RectangleF upper = new RectangleF(10 * scale, 3 * scale, 12 * scale, 12 * scale);
            RectangleF lower = new RectangleF(10 * scale, 17 * scale, 12 * scale, 12 * scale);
            using (var track = new SolidBrush(Color.FromArgb(48, 63, 82)))
            { graphics.FillEllipse(track, upper); graphics.FillEllipse(track, lower); }
            using (var quota = new SolidBrush(Color.FromArgb(58, 190, 215)))
                graphics.FillPie(quota, upper.X, upper.Y, upper.Width, upper.Height, -90, 270);
            using (var time = new SolidBrush(Color.FromArgb(51, 154, 197)))
                graphics.FillPie(time, lower.X, lower.Y, lower.Width, lower.Height, -90, 200);
            // Premultiplied source pixels keep transparent edges free of a white matte.
            resized.Clear(Color.Transparent);
            resized.CompositingMode = CompositingMode.SourceCopy;
            resized.CompositingQuality = CompositingQuality.HighQuality;
            resized.InterpolationMode = InterpolationMode.HighQualityBicubic;
            resized.PixelOffsetMode = PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            resized.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            bitmap.Save(memory, ImageFormat.Png);
            frames.Add(memory.ToArray());
        }
        using (var stream = File.Create(args[0]))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
            int offset = 6 + sizes.Length * 16;
            for (int i = 0; i < sizes.Length; i++)
            {
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
            }
            foreach (byte[] frame in frames) writer.Write(frame);
        }
    }
}
