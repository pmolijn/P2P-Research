namespace NetworkWrapper.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    public static class Security
    {
        public static bool DllHijackingAttempted(string path, string dllFileName)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            return File.Exists(info.FullName + Path.DirectorySeparatorChar + dllFileName);
        }

        public static bool DllHijackingAttempted(IList<string> paths, IList<string> dllFileNames, out string hijackedPath)
        {
            char[] trimChars = new char[] { Path.DirectorySeparatorChar };
            foreach (string str in paths)
            {
                if (!str.Trim(trimChars).EndsWith("system32"))
                {
                    foreach (string str2 in dllFileNames)
                    {
                        if (DllHijackingAttempted(str, str2))
                        {
                            hijackedPath = str + Path.DirectorySeparatorChar + str2;
                            return true;
                        }
                    }
                }
            }
            hijackedPath = "";
            return false;
        }
    }
}

