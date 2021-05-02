using System.IO;

namespace Codebelt.Cdn.Origin
{
    public static class StreamExtensions
    {
        public static byte[] ToByteArray(this Stream file, int bytesToRead)
        {
            long buffer = bytesToRead;
            if (file.Length < buffer) { buffer = file.Length; }

            var checksumBytes = new byte[buffer];
            using (file)
            {
                file.Read(checksumBytes, 0, (int)buffer);
            }
            return checksumBytes;
        }
    }
}