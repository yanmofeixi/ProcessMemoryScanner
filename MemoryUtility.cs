namespace ProcessMemoryScanner
{
    public static class MemoryUtility
    {        
        public static bool BytesMatch(byte[] A, byte[] B)
        {
            if(A.Length != B.Length)
            {
                return false;
            }
            for(var i = 0; i < A.Length; i++)
            {
                if(A[i] != B[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static byte[] SubBytes(this byte[] data, int startIndex, int Length)
        {
            var result = new byte[Length];
            for (var i = 0; i < Length; i++)
            {
                result[i] = data[i + startIndex];
            }
            return result;
        }

        public static byte?[] SubBytesWithWildCard(this byte?[] data, int startIndex, int Length)
        {
            var result = new byte?[Length];
            for (var i = 0; i < Length; i++)
            {
                result[i] = data[i + startIndex];
            }
            return result;
        }

        public static int IndexOf(this byte[] bytes, byte[] subBytes)
        {
            var index = -1;
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == subBytes[0])
                {
                    var match = true;
                    for (var j = 1; j < subBytes.Length; j++)
                    {
                        if (bytes[i + j] != subBytes[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        index = i;
                        break;
                    }
                }
            }
            return index;
        }

        public static int IndexOfWithWildCard(this byte?[] bytes, byte?[] subBytes)
        {
            var index = -1;
            for (var i = 0; i < bytes.Length; i++)
            {
                if (WildCardMatch(bytes[i], subBytes[0]))
                {
                    var match = true;
                    for (var j = 1; j < subBytes.Length; j++)
                    {
                        if (!WildCardMatch(bytes[i + j], subBytes[j]))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        index = i;
                        break;
                    }
                }
            }
            return index;
        }

        public static byte?[] ToWildCardByteArray(this byte[] bytes)
        {
            var result = new byte?[bytes.Length];
            for(var i = 0; i < bytes.Length; i++)
            {
                result[i] = bytes[i];
            }
            return result;
        }

        /// <summary>
        /// A==B || A == null || B == null
        /// </summary>
        public static bool WildCardMatch(byte? A, byte? B)
        {
            return (A == null) || (B == null) || A == B;
        }
    }
}
