namespace pcapFileIO
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class Md5SingletonHelper
    {
        private static Md5SingletonHelper instance;
        private MD5 md5 = MD5.Create();

        private Md5SingletonHelper()
        {
        }

        public string GetMd5Sum(string file)
        {
            byte[] buffer;
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 0x40000, FileOptions.SequentialScan))
            {
                buffer = this.md5.ComputeHash(stream);
            }
            StringBuilder builder = new StringBuilder();
            foreach (byte num in buffer)
            {
                builder.Append(num.ToString("X2").ToLower());
            }
            return builder.ToString();
        }

        public static Md5SingletonHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Md5SingletonHelper();
                }
                return instance;
            }
        }
    }
}

