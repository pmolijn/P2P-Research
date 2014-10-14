namespace PacketParser.Mime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal static class PartBuilder
    {
        public static IEnumerable<MultipartPart> GetParts(UnbufferedReader streamReader)
        {
            NameValueCollection iteratorVariable1;
            long position = streamReader.BaseStream.Position;
            MultipartPart.ReadHeaderAttributes(streamReader.BaseStream, streamReader.BaseStream.Position, out iteratorVariable1);
            string boundary = iteratorVariable1["boundary"];
            if (boundary != null)
            {
                streamReader.BaseStream.Position = position;
                foreach (MultipartPart iteratorVariable3 in GetParts(streamReader, boundary))
                {
                    yield return iteratorVariable3;
                }
            }
            else
            {
                yield return new MultipartPart(streamReader.BaseStream, streamReader.BaseStream.Position, (int) (streamReader.BaseStream.Length - streamReader.BaseStream.Position));
            }
        }

        public static IEnumerable<MultipartPart> GetParts(byte[] mimeMultipartData, string boundary)
        {
            Stream stream = new ByteArrayStream(mimeMultipartData, 0L);
            UnbufferedReader streamReader = new UnbufferedReader(stream);
            return GetParts(streamReader, boundary);
        }

        public static IEnumerable<MultipartPart> GetParts(UnbufferedReader streamReader, string boundary)
        {
            string iteratorVariable0 = "--" + boundary;
            string iteratorVariable1 = "--" + boundary + "--";
            while (!streamReader.EndOfStream)
            {
                long position = streamReader.BaseStream.Position;
                int iteratorVariable3 = 0;
                string iteratorVariable4 = streamReader.ReadLine(200);
                while ((iteratorVariable4 != iteratorVariable0) && (iteratorVariable4 != iteratorVariable1))
                {
                    iteratorVariable3 = (int) ((streamReader.BaseStream.Position - 2L) - position);
                    iteratorVariable4 = streamReader.ReadLine(200);
                    if (iteratorVariable4 == null)
                    {
                        break;
                    }
                }
                long iteratorVariable5 = streamReader.BaseStream.Position;
                if (iteratorVariable3 > 0)
                {
                    byte[] buffer = new byte[iteratorVariable3];
                    streamReader.BaseStream.Position = position;
                    streamReader.BaseStream.Read(buffer, 0, buffer.Length);
                    MultipartPart iteratorVariable7 = new MultipartPart(buffer);
                    if (((iteratorVariable7.Attributes["Content-Type"] != null) && iteratorVariable7.Attributes["Content-Type"].Contains("multipart")) && ((iteratorVariable7.Attributes["boundary"] != null) && (iteratorVariable7.Attributes["boundary"] != boundary)))
                    {
                        foreach (MultipartPart iteratorVariable8 in GetParts(iteratorVariable7.Data, iteratorVariable7.Attributes["boundary"]))
                        {
                            yield return iteratorVariable8;
                        }
                    }
                    else
                    {
                        yield return iteratorVariable7;
                    }
                }
                streamReader.BaseStream.Position = iteratorVariable5;
                if (iteratorVariable4 == iteratorVariable1)
                {
                    break;
                }
            }
        }


    }
}

