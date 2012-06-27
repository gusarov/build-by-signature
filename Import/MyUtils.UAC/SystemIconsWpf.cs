using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyUtils.UAC
{
	public static class SystemIconsWpf
	{
		[DllImport("gdi32", SetLastError = true)]
		static extern bool DeleteObject(IntPtr hObject);

		public static ImageSource ToImageSource(this Icon icon, int? size = null)
		{
			var bitmap = icon.ToBitmap();
			var hBitmap = bitmap.GetHbitmap();

			var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
				hBitmap,
				IntPtr.Zero,
				Int32Rect.Empty,
				size.HasValue ? BitmapSizeOptions.FromWidthAndHeight(size.Value, size.Value) : BitmapSizeOptions.FromEmptyOptions());

			DeleteObject(hBitmap);

			return wpfBitmap;
		}

		static readonly Lazy<ImageSource> _shield16 = new Lazy<ImageSource>(() => SystemIcons.Shield.ToImageSource(16));

		public static ImageSource Shield16
		{
			get { return _shield16.Value; }
		}

		static readonly Lazy<ImageSource> _shield32 = new Lazy<ImageSource>(() => SystemIcons.Shield.ToImageSource(32));

		public static ImageSource Shield32
		{
			get { return _shield32.Value; }
		}
	}
}
