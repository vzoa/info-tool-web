using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ZoaReference.Features.Charts.Services;

public class PdfRotationDetector
{
    public int DetectRotation(byte[] pdfBytes)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);

            var orientationCounts = new Dictionary<TextOrientation, int>();

            foreach (var page in document.GetPages())
            {
                foreach (var letter in page.Letters)
                {
                    if (string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    orientationCounts.TryGetValue(letter.TextOrientation, out var count);
                    orientationCounts[letter.TextOrientation] = count + 1;
                }
            }

            if (orientationCounts.Count == 0)
            {
                return 0;
            }

            var dominant = orientationCounts
                .OrderByDescending(kv => kv.Value)
                .First()
                .Key;

            return dominant switch
            {
                TextOrientation.Rotate90 => 90,
                TextOrientation.Rotate270 => -90,
                TextOrientation.Rotate180 => 180,
                _ => 0
            };
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
