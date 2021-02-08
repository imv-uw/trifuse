using System.IO;

namespace Core.Tools
{
    public class FileSystem
    {
        public static string GetBasename(string file, bool removeDotExtension = false)
        {
            string baseName = (new FileInfo(file)).Name;

            if (removeDotExtension)
            {
                int dotIndex = baseName.LastIndexOf('.');
                if (dotIndex > 0)
                    return baseName.Substring(0, dotIndex);
            }

            return baseName;
        }
    }
}
