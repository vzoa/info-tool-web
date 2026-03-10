using UglyToad.PdfPig;

namespace ZoaReference.Features.Charts.Services;

/// <summary>
/// Detects PDF text rotation by analyzing baseline angles of individual letters,
/// matching the CLI's atan2-based transformation matrix approach.
/// </summary>
public class PdfRotationDetector
{
    public int DetectRotation(byte[] pdfBytes)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);

            var angleCounts = new Dictionary<int, int>();

            foreach (var page in document.GetPages())
            {
                foreach (var letter in page.Letters)
                {
                    if (string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    var dx = letter.EndBaseLine.X - letter.StartBaseLine.X;
                    var dy = letter.EndBaseLine.Y - letter.StartBaseLine.Y;

                    if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
                    {
                        continue;
                    }

                    var rawAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                    var rounded = (int)(Math.Round(rawAngle / 10.0) * 10);
                    angleCounts.TryGetValue(rounded, out var count);
                    angleCounts[rounded] = count + 1;
                }
            }

            if (angleCounts.Count == 0)
            {
                return 0;
            }

            var upright = CountBucket(angleCounts, [-10, 0, 10]);
            var rotated90 = CountBucket(angleCounts, [80, 90, 100]);
            var rotatedNeg90 = CountBucket(angleCounts, [-80, -90, -100]);

            if (rotated90 > upright && rotated90 >= rotatedNeg90)
            {
                return 90;
            }

            if (rotatedNeg90 > upright && rotatedNeg90 > rotated90)
            {
                return -90;
            }

            return 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static int CountBucket(
        Dictionary<int, int> counts, int[] angles)
    {
        var total = 0;
        foreach (var angle in angles)
        {
            if (counts.TryGetValue(angle, out var count))
            {
                total += count;
            }
        }

        return total;
    }
}
