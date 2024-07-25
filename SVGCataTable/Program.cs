using System;
using System.IO;
using System.Collections.Generic;
using SkiaSharp;
using Svg.Skia;
using static System.Net.Mime.MediaTypeNames;

namespace SvgProcessingApp
{
    class Program
    {
        static float thumbnailSizeInches = 1.5f;
        static string directoryPath = ".";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SvgProcessingApp <directoryPath> [thumbnailSizeInches]");
                thumbnailSizeInches = args.Length > 1 ? float.Parse(args[1]) : 1.5f;
                directoryPath = args[0];
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory does not exist: {directoryPath}");
                return;
            }

            string[] svgFiles = Directory.GetFiles(directoryPath, "*.svg", SearchOption.AllDirectories);
            if (svgFiles.Length == 0)
            {
                Console.WriteLine("No SVG files found to process.");
                return;
            }

            int dpi = 600;
            float inchToPixel = dpi / 2.54f; // Convert DPI to pixels per inch
            int thumbnailSize = (int)(thumbnailSizeInches * inchToPixel);

            string catalogDirectory = Path.Combine(directoryPath, "catalogue");
            Directory.CreateDirectory(catalogDirectory);

            List<string> htmlRows = new List<string>();
            int fileCounter = 1;

            foreach (var svgFilePath in svgFiles)
            {
                var svg = new SKSvg();
                svg.Load(svgFilePath);

                if (svg.Picture != null)
                {
                    float originalWidth = svg.Picture.CullRect.Width;
                    float originalHeight = svg.Picture.CullRect.Height;
                    float scale = Math.Min((float)thumbnailSize / originalWidth, (float)thumbnailSize / originalHeight);

                    using (var bitmap = SKPictureExtensions.ToBitmap(svg.Picture, SKColors.Transparent, scale, scale, SKColorType.Rgba8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb()))
                    {
                        string fileName = $"thumb_{fileCounter}.jpg";
                        string outputFilePath = Path.Combine(catalogDirectory, fileName);

                        using (var image = SKImage.FromBitmap(bitmap))
                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 100))
                        using (var stream = File.OpenWrite(outputFilePath))
                        {
                            data.SaveTo(stream);
                        }

                        string relativeFilePath = Path.Combine("catalogue", fileName);
                        htmlRows.Add($@"
                        <tr>
                            <td><a href=""{relativeFilePath}""><img src=""{relativeFilePath}"" alt=""{fileName}"" style=""width:100px;height:auto;""></a></td>
                            <td>{fileName}</td>
                        </tr>");

                        fileCounter++;
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to load SVG file: {svgFilePath}");
                }
            }

            string htmlContent = $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>SVG Thumbnails</title>
                <style>
                    table {{
                        width: 100%;
                        border-collapse: collapse;
                    }}
                    th, td {{
                        border: 1px solid black;
                        padding: 10px;
                        text-align: center;
                    }}
                    img {{
                        width: 100px;
                        height: auto;
                    }}
                </style>
            </head>
            <body>
                <table>
                    <thead>
                        <tr>
                            <th>Thumbnail</th>
                            <th>File Name</th>
                        </tr>
                    </thead>
                    <tbody>
                        {string.Join("\n", htmlRows)}
                    </tbody>
                </table>
            </body>
            </html>";

            File.WriteAllText(Path.Combine(directoryPath, "index.html"), htmlContent);
            Console.WriteLine("HTML file generated successfully.");
        }
    }
}
