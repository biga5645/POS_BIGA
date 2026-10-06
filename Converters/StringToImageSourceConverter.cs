using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using POS_BIGA.Infrastructure.Helpers;

namespace POS_BIGA.Converters
{
    /// <summary>
    /// Converts a string (URL or local path) to a BitmapImage safely.
    /// ✅ Optimized with DecodePixelWidth for memory efficiency
    /// Returns null when input is null/empty or on failure.
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string s)) return null;
            if (string.IsNullOrWhiteSpace(s)) return null;

            try
            {
                s = s.Trim();

                Uri uri = null;

                // If value is a relative server-side images path like /Images/Products/xxx.jpg
                if (s.StartsWith("/Images/", StringComparison.OrdinalIgnoreCase))
                {
                    // Map to physical file under central images root
                    var relative = s.Substring("/Images/".Length).Replace('/', Path.DirectorySeparatorChar);
                    var imagesRoot = ImageStorageHelper.GetCentralImagesRootPath();
                    var filePath = Path.Combine(imagesRoot, relative);
                    if (File.Exists(filePath))
                    {
                        // Load from file directly
                        return LoadBitmapFromFile(filePath);
                    }
                    else
                    {
                        // fallback: try to map using existing helper
                        var mapped = ImageStorageHelper.MapToCentralFileUri(s);
                        if (!string.IsNullOrWhiteSpace(mapped) && Uri.TryCreate(mapped, UriKind.Absolute, out var mu) && mu.IsFile)
                            return LoadBitmapFromFile(mu.LocalPath);
                    }
                }
                else if (s.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    var mapped = ImageStorageHelper.MapToCentralFileUri(s);
                    if (!string.IsNullOrWhiteSpace(mapped) && Uri.TryCreate(mapped, UriKind.Absolute, out var mu) && mu.IsFile)
                        return LoadBitmapFromFile(mu.LocalPath);
                    // else try as HTTP URL
                    if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var relU))
                    {
                        if (relU.IsAbsoluteUri)
                            uri = relU;
                    }
                }
                else if (s.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(s, UriKind.Absolute, out var fu) && fu.IsFile)
                    {
                        return LoadBitmapFromFile(fu.LocalPath);
                    }
                }
                else if (Path.IsPathRooted(s))
                {
                    // Local absolute path like C:\...
                    if (File.Exists(s))
                    {
                        return LoadBitmapFromFile(s);
                    }
                }
                else if (Uri.TryCreate(s, UriKind.Absolute, out var au))
                {
                    uri = au;
                }
                else
                {
                    // relative path or other: try to resolve against central images root
                    try
                    {
                        var imagesRoot = ImageStorageHelper.GetCentralImagesRootPath();
                        var candidate = Path.Combine(imagesRoot, s.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(candidate))
                        {
                            return LoadBitmapFromFile(candidate);
                        }
                        else if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var relU2))
                        {
                            uri = relU2;
                        }
                    }
                    catch
                    {
                        // fall through
                    }
                }

                if (uri == null) return null;

                // For HTTP/HTTPS URIs load via UriSource
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = uri;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 400; // allow larger preview in desktop app
                    bmp.CreateOptions = BitmapCreateOptions.None;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch
                {
                    return null;
                }
            }
            catch
            {
                // return null when conversion fails (broken URL, missing file, etc.)
                return null;
            }
        }

        private BitmapImage LoadBitmapFromFile(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = fs;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 400;
                bmp.CreateOptions = BitmapCreateOptions.None;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
