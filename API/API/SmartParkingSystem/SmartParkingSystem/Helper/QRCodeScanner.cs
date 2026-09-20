using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Windows.Compatibility;

namespace SmartParkingSystem.Helper
{
    public class QRCodeScanner
    {
        public static string GetJsonFromQrCode(byte[] imageBytes)
        {
            try
            {
                using (var ms = new MemoryStream(imageBytes))
                using (var originalBitmap = new Bitmap(ms))
                using (var grayscaleBitmap = ConvertToGrayscale(originalBitmap))
                {
                    var reader = new BarcodeReader
                    {
                        AutoRotate = true,
                        TryInverted = true
                    };

                    var result = reader.Decode(grayscaleBitmap);
                    return result?.Text;
                }
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                var colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });

                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return newBitmap;
        }
    }
}
