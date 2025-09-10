using System;
using System.IO;
using System.Reflection;

namespace OkoloXray.Utilities
{
    internal class LibLoader
    {
        public static string ExtractDll(string dllName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"OkoloXray.Assets.Libraries.{dllName}";

            string outputPath = Path.Combine(AppContext.BaseDirectory, dllName);

            if (!File.Exists(outputPath))
            {
                var names = assembly.GetManifestResourceNames();

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new Exception($"Embedded resource {resourceName} not found.");

                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }

            return outputPath;
        }
    }
}
