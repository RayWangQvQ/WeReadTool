using System.Diagnostics;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp.Rendering;
using ZXing.SkiaSharp;

namespace WeReadTool.Tests
{
    public class QrCodeTest
    {
        [Fact]
        public string Decode()
        {
            var base64 = "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";
            byte[] arr = Convert.FromBase64String(base64);
            using MemoryStream ms = new MemoryStream(arr);
            using SKBitmap? skiaImage = SkiaSharp.SKBitmap.Decode(ms);

            var skiaReader = new ZXing.SkiaSharp.BarcodeReader();
            var skiaResult = skiaReader.Decode(skiaImage);

            return skiaResult.Text;
        }

        [Fact]
        public void Encode()
        {
            // 生成二维码图片
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 20,
                    Height = 20,
                    Margin = 1
                },
                Renderer = new SKBitmapRenderer()
            };
            SKBitmap? bitmap = writer.Write("Hello World!");

            var pixels = bitmap.Pixels;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = pixels[y * bitmap.Width + x];

                    //Console.ForegroundColor = ConsoleColor.Black;
                    //Console.BackgroundColor = (pixel == 0xFFFFFFFF) ? ConsoleColor.White : ConsoleColor.Black;
                    //Console.Write("  ");

                    if (pixel == 0xFFFFFFFF)
                    {
                        Debug.Write("\u2588\u2588"); //"██" 不知道为啥，打印出来实际宽会缩半，需要两个
                    }
                    else
                    {
                        Debug.Write("\uFF07"); //"＇"
                    }
                }
                Debug.WriteLine("");
            }
        }
    }
}