using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace XControl
{
    internal static class StringUtil
    {
        // Methods
        internal static string CheckAndTrimString(string paramValue, string paramName)
        {
            return CheckAndTrimString(paramValue, paramName, true);
        }

        internal static string CheckAndTrimString(string paramValue, string paramName, bool throwIfNull)
        {
            return CheckAndTrimString(paramValue, paramName, throwIfNull, -1);
        }

        internal static string CheckAndTrimString(string paramValue, string paramName, bool throwIfNull, int lengthToCheck)
        {
            if (paramValue == null)
            {
                if (throwIfNull)
                {
                    throw new ArgumentNullException(paramName);
                }
                return null;
            }
            string str = paramValue.Trim();
            if (str.Length == 0)
            {
                throw new ArgumentException(SR.GetString("PersonalizationProviderHelper_TrimmedEmptyString", new object[] { paramName }));
            }
            if ((lengthToCheck > -1) && (str.Length > lengthToCheck))
            {
                throw new ArgumentException(SR.GetString("StringUtil_Trimmed_String_Exceed_Maximum_Length", new object[] { paramValue, paramName, lengthToCheck.ToString(CultureInfo.InvariantCulture) }));
            }
            return str;
        }

        internal static bool Equals(string s1, string s2)
        {
            return ((s1 == s2) || (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)));
        }

        internal static bool EqualsIgnoreCase(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            {
                return true;
            }
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            {
                return false;
            }
            if (s2.Length != s1.Length)
            {
                return false;
            }
            return (0 == string.Compare(s1, 0, s2, 0, s2.Length, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool EqualsIgnoreCase(string s1, int index1, string s2, int index2, int length)
        {
            return (string.Compare(s1, index1, s2, index2, length, StringComparison.OrdinalIgnoreCase) == 0);
        }

        //internal static unsafe int GetStringHashCode(string s)
        //{
        //    fixed (char* str = ((char*)s))
        //    {
        //        char* chPtr = str;
        //        int num = 0x15051505;
        //        int num2 = num;
        //        int* numPtr = (int*)chPtr;
        //        for (int i = s.Length; i > 0; i -= 4)
        //        {
        //            num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
        //            if (i <= 2)
        //            {
        //                break;
        //            }
        //            num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
        //            numPtr += 2;
        //        }
        //        return (num + (num2 * 0x5d588b65));
        //    }
        //}

        internal static unsafe void memcpyimpl(byte* src, byte* dest, int len)
        {
            if (len >= 0x10)
            {
                do
                {
                    *((int*)dest) = *((int*)src);
                    *((int*)(dest + 4)) = *((int*)(src + 4));
                    *((int*)(dest + 8)) = *((int*)(src + 8));
                    *((int*)(dest + 12)) = *((int*)(src + 12));
                    dest += 0x10;
                    src += 0x10;
                }
                while ((len -= 0x10) >= 0x10);
            }
            if (len > 0)
            {
                if ((len & 8) != 0)
                {
                    *((int*)dest) = *((int*)src);
                    *((int*)(dest + 4)) = *((int*)(src + 4));
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0)
                {
                    *((int*)dest) = *((int*)src);
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0)
                {
                    *((short*)dest) = *((short*)src);
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0)
                {
                    dest++;
                    src++;
                    dest[0] = src[0];
                }
            }
        }

        internal static string[] ObjectArrayToStringArray(object[] objectArray)
        {
            string[] array = new string[objectArray.Length];
            objectArray.CopyTo(array, 0);
            return array;
        }

        internal static bool StringArrayEquals(string[] a, string[] b)
        {
            if ((a == null) != (b == null))
            {
                return false;
            }
            if (a != null)
            {
                int length = a.Length;
                if (length != b.Length)
                {
                    return false;
                }
                for (int i = 0; i < length; i++)
                {
                    if (a[i] != b[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal static bool StringEndsWith(string s, char c)
        {
            int length = s.Length;
            return ((length != 0) && (s[length - 1] == c));
        }

        //internal static unsafe bool StringEndsWith(string s1, string s2)
        //{
        //    int num = s1.Length - s2.Length;
        //    if (num < 0)
        //    {
        //        return false;
        //    }
        //    fixed (char* str = ((char*)s1))
        //    {
        //        char* chPtr = str;
        //        fixed (char* str2 = ((char*)s2))
        //        {
        //            char* chPtr2 = str2;
        //            char* chPtr3 = chPtr + num;
        //            char* chPtr4 = chPtr2;
        //            int length = s2.Length;
        //            while (length-- > 0)
        //            {
        //                chPtr3++;
        //                chPtr4++;
        //                if (chPtr3[0] != chPtr4[0])
        //                {
        //                    return false;
        //                }
        //            }
        //        }
        //    }
        //    return true;
        //}

        internal static bool StringEndsWithIgnoreCase(string s1, string s2)
        {
            int indexA = s1.Length - s2.Length;
            if (indexA < 0)
            {
                return false;
            }
            return (0 == string.Compare(s1, indexA, s2, 0, s2.Length, StringComparison.OrdinalIgnoreCase));
        }

        internal static string StringFromCharPtr(IntPtr ip, int length)
        {
            return Marshal.PtrToStringAnsi(ip, length);
        }

        internal static unsafe string StringFromWCharPtr(IntPtr ip, int length)
        {
            return new string((char*)ip, 0, length);
        }

        internal static bool StringStartsWith(string s, char c)
        {
            return ((s.Length != 0) && (s[0] == c));
        }

        //internal static unsafe bool StringStartsWith(string s1, string s2)
        //{
        //    if (s2.Length > s1.Length)
        //    {
        //        return false;
        //    }
        //    fixed (char* str = ((char*)s1))
        //    {
        //        char* chPtr = str;
        //        fixed (char* str2 = ((char*)s2))
        //        {
        //            char* chPtr2 = str2;
        //            char* chPtr3 = chPtr;
        //            char* chPtr4 = chPtr2;
        //            int length = s2.Length;
        //            while (length-- > 0)
        //            {
        //                chPtr3++;
        //                chPtr4++;
        //                if (chPtr3[0] != chPtr4[0])
        //                {
        //                    return false;
        //                }
        //            }
        //        }
        //    }
        //    return true;
        //}

        internal static bool StringStartsWithIgnoreCase(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            {
                return false;
            }
            if (s2.Length > s1.Length)
            {
                return false;
            }
            return (0 == string.Compare(s1, 0, s2, 0, s2.Length, StringComparison.OrdinalIgnoreCase));
        }

        //internal static unsafe void UnsafeStringCopy(string src, int srcIndex, char[] dest, int destIndex, int len)
        //{
        //    int num = len * 2;
        //    fixed (char* str = ((char*)src))
        //    {
        //        char* chPtr = str;
        //        fixed (char* chRef = dest)
        //        {
        //            byte* numPtr = (byte*)(chPtr + srcIndex);
        //            byte* numPtr2 = (byte*)(chRef + destIndex);
        //            memcpyimpl(numPtr, numPtr2, num);
        //        }
        //    }
        //}
    }
}
