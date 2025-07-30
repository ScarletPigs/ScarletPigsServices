namespace ScarletPigsServices.Website.Data.Models.Helpers
{
    public static class FileSizeFormatterUtil
    {
        const double KB = 1000; // 1 KB = 1024 B
        const double MB = KB * 1000; // 1 MB = 1024 KB
        const double GB = MB * 1000; // 1 GB = 1024 MB

        public static string ReadableSizeFromBytes(long sizeInBytes)
        {
            string formattedSize;

            if (sizeInBytes < KB)
            {
                formattedSize = $"{sizeInBytes} Bytes"; // For sizes between 1 B and 1 KB
            }
            else if (sizeInBytes < MB)
            {
                double sizeInKB = sizeInBytes / KB;
                formattedSize = $"{sizeInKB:F3} KB"; // For sizes between 1 KB and 1 MB
            }
            else if (sizeInBytes < GB)
            {
                double sizeInMB = sizeInBytes / MB;
                formattedSize = $"{sizeInMB:F3} MB"; // For sizes between 1 MB and 1 GB
            }
            else
            {
                double sizeInGB = sizeInBytes / GB;
                formattedSize = $"{sizeInGB:F3} GB"; // For sizes above 1 GB
            }

            return formattedSize;
        }
    }
}