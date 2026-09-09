using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    internal static class LayeredSurface
    {
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; public NativePoint(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width, Height; public NativeSize(int width, int height) { Width = width; Height = height; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { public byte Operation, Flags, ConstantAlpha, AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)] private struct BitmapHeader { public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, ImageSize; public int XPels, YPels; public uint ColorsUsed, ColorsImportant; }
        [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { public BitmapHeader Header; public uint Colors; }
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr item);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr item);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo info, uint usage, out IntPtr bits, IntPtr section, uint offset);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destination, ref NativePoint position, ref NativeSize size, IntPtr source, ref NativePoint origin, uint key, ref Blend blend, uint flags);

        internal static void Present(Form window, Bitmap image)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, bitmap = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                if (screen == IntPtr.Zero) throw new Win32Exception();
                memory = CreateCompatibleDC(screen);
                if (memory == IntPtr.Zero) throw new Win32Exception();
                var info = new BitmapInfo();
                info.Header.Size = (uint)Marshal.SizeOf(typeof(BitmapHeader));
                info.Header.Width = image.Width; info.Header.Height = -image.Height;
                info.Header.Planes = 1; info.Header.BitCount = 32;
                info.Header.ImageSize = (uint)(image.Width * image.Height * 4);
                IntPtr pixels;
                bitmap = CreateDIBSection(memory, ref info, 0, out pixels, IntPtr.Zero, 0);
                if (bitmap == IntPtr.Zero || pixels == IntPtr.Zero) throw new Win32Exception();
                var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                try
                {
                    byte[] row = new byte[image.Width * 4];
                    for (int y = 0; y < image.Height; y++)
                    {
                        Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                        Marshal.Copy(row, 0, IntPtr.Add(pixels, y * row.Length), row.Length);
                    }
                }
                finally { image.UnlockBits(data); }
                previous = SelectObject(memory, bitmap);
                if (previous == IntPtr.Zero || previous == new IntPtr(-1)) throw new Win32Exception();
                var position = new NativePoint(window.Left, window.Top);
                var origin = new NativePoint(0, 0);
                var size = new NativeSize(image.Width, image.Height);
                var blend = new Blend { ConstantAlpha = 255, AlphaFormat = 1 };
                if (!UpdateLayeredWindow(window.Handle, screen, ref position, ref size, memory, ref origin, 0, ref blend, 2))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (previous != IntPtr.Zero && previous != new IntPtr(-1)) SelectObject(memory, previous);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
}
