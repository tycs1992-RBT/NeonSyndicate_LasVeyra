namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free line ending rules for the art receipts. A receipt records the SHA-256 of each delivered file as Git stores
    /// it, so text files (the .json motion sets and export statistics a receipt lists) are fingerprinted with LF line
    /// endings. Git for Windows checks text files out with CRLF endings, which changes their bytes but not their content.
    /// These rules let the Editor check recognise that case exactly the way Git itself decides what is text: a file is text
    /// when its first 8000 bytes hold no zero byte, and only CR LF pairs turn back into LF (a lone CR stays). Binary art
    /// (FBX, PNG, JPG, Blender files) always holds zero bytes near its start, so it is only ever compared byte for byte.
    /// </summary>
    public static class ReceiptLineEndings
    {
        /// <summary>How many leading bytes Git inspects for a zero byte before it calls a file binary.</summary>
        public const int TextProbe = 8000;

        /// <summary>True when the first length bytes look like text to Git: no zero byte within the first 8000.</summary>
        public static bool LooksText(byte[] data, int length)
        {
            if (data == null || length < 0 || length > data.Length) return false;
            int probe = length < TextProbe ? length : TextProbe;
            for (int i = 0; i < probe; i++)
                if (data[i] == 0) return false;
            return true;
        }

        /// <summary>True when the first length bytes contain at least one CR LF pair.</summary>
        public static bool HasCrLf(byte[] data, int length)
        {
            if (data == null || length < 2 || length > data.Length) return false;
            for (int i = 0; i + 1 < length; i++)
                if (data[i] == 13 && data[i + 1] == 10) return true;
            return false;
        }

        /// <summary>
        /// A copy of the first length bytes with every CR LF pair turned into LF, exactly what Git stores for a text file
        /// that Windows checked out with CRLF endings. A lone CR is kept. Returns an empty array for invalid input.
        /// </summary>
        public static byte[] ToLf(byte[] data, int length)
        {
            if (data == null || length < 0 || length > data.Length) return new byte[0];
            int pairs = 0;
            for (int i = 0; i + 1 < length; i++)
                if (data[i] == 13 && data[i + 1] == 10) pairs++;
            byte[] result = new byte[length - pairs];
            int w = 0;
            for (int i = 0; i < length; i++)
            {
                if (data[i] == 13 && i + 1 < length && data[i + 1] == 10) continue;
                result[w++] = data[i];
            }
            return result;
        }
    }
}
