#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.ImageEditor.Core.Annotations;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using XerahS.RegionCapture.Services;

namespace XerahS.RegionCapture.UI;

public partial class OverlayWindow
{
    #region Color Sampling

    /// <summary>
    /// Attempts to resolve SmartEraser color using a robust fallback chain:
    /// 1) Shared EditorCore snapshot sampling,
    /// 2) Full virtual-screen background bitmap with monitor mapping,
    /// 3) Editor source image,
    /// 4) Windows live-screen sampling (last resort).
    /// </summary>
    private string? ResolveSmartEraserColor(SKPoint logicalPoint)
    {
        string? sharedSample = _viewModel.EditorCore.SampleCanvasColor(logicalPoint);
        if (!string.IsNullOrWhiteSpace(sharedSample))
        {
            return sharedSample;
        }

        if (TrySampleVirtualBackgroundColor(logicalPoint, out string? virtualColor))
        {
            return virtualColor;
        }

        if (TrySampleBitmapColor(_viewModel.EditorCore.SourceImage, logicalPoint, out string? sourceColor))
        {
            return sourceColor;
        }


        return null;
    }

    private bool TrySampleVirtualBackgroundColor(SKPoint logicalPoint, out string? color)
    {
        color = null;
        if (_backgroundBitmap == null || _backgroundBitmap.Width <= 0 || _backgroundBitmap.Height <= 0)
        {
            return false;
        }

        int physX = (int)Math.Round(logicalPoint.X * _monitor.ScaleFactor);
        int physY = (int)Math.Round(logicalPoint.Y * _monitor.ScaleFactor);

        var coordService = new CoordinateTranslationService();
        var virtualBounds = coordService.GetVirtualScreenBounds();
        int bmpX = (int)Math.Round(_monitor.PhysicalBounds.X - virtualBounds.X) + physX;
        int bmpY = (int)Math.Round(_monitor.PhysicalBounds.Y - virtualBounds.Y) + physY;
        bmpX = Math.Clamp(bmpX, 0, _backgroundBitmap.Width - 1);
        bmpY = Math.Clamp(bmpY, 0, _backgroundBitmap.Height - 1);

        var pixel = _backgroundBitmap.GetPixel(bmpX, bmpY);
        color = ToRgbHex(pixel);
        return true;
    }


    private static bool TrySampleBitmapColor(SKBitmap? bitmap, SKPoint logicalPoint, out string? color)
    {
        color = null;

        if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return false;
        }

        int x = Math.Clamp((int)Math.Round(logicalPoint.X), 0, bitmap.Width - 1);
        int y = Math.Clamp((int)Math.Round(logicalPoint.Y), 0, bitmap.Height - 1);
        var pixel = bitmap.GetPixel(x, y);
        color = ToRgbHex(pixel);
        return true;
    }

    private static string ToRgbHex(SKColor color)
    {
        return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }

    #endregion
}
