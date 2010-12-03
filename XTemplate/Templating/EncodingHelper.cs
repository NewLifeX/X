namespace Microsoft.VisualStudio.TextTemplating
{
    using System;
    using System.IO;
    using System.Text;

    public static class EncodingHelper
    {
        public static Encoding GetEncoding(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }
            Encoding currentEncoding = Encoding.Default;
            if (!File.Exists(filePath))
            {
                return currentEncoding;
            }
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (stream.Length > 0L)
                    {
                        using (StreamReader reader = new StreamReader(stream, true))
                        {
                            char[] chArray = new char[1];
                            reader.Read(chArray, 0, 1);
                            currentEncoding = reader.CurrentEncoding;
                            reader.BaseStream.Position = 0L;
                            if (currentEncoding == Encoding.UTF8)
                            {
                                byte[] preamble = currentEncoding.GetPreamble();
                                if (stream.Length >= preamble.Length)
                                {
                                    byte[] buffer = new byte[preamble.Length];
                                    stream.Read(buffer, 0, buffer.Length);
                                    for (int i = 0; i < buffer.Length; i++)
                                    {
                                        if (buffer[i] != preamble[i])
                                        {
                                            currentEncoding = Encoding.Default;
                                            goto Label_00EF;
                                        }
                                    }
                                }
                                else
                                {
                                    currentEncoding = Encoding.Default;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                if (Engine.IsCriticalException(exception))
                {
                    throw;
                }
            }
        Label_00EF:
            if (currentEncoding == null)
            {
                currentEncoding = Encoding.UTF8;
            }
            return currentEncoding;
        }
    }
}

