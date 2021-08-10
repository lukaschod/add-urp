using System;
using System.IO;
using System.Text;

namespace add_urp
{
    public static class Ext
    {
        // https://stackoverflow.com/questions/1359948/why-doesnt-stringbuilder-have-indexof-method
        /// <summary>
        /// Returns the index of the start of the contents in a StringBuilder
        /// </summary>        
        /// <param name="value">The string to find</param>
        /// <param name="startIndex">The starting index.</param>
        /// <param name="ignoreCase">if set to <c>true</c> it will ignore case</param>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
        {            
            int index;
            int length = value.Length;
            int maxSearchLength = (sb.Length - length) + 1;

            if (ignoreCase)
            {
                for (int i = startIndex; i < maxSearchLength; ++i)
                {
                    if (Char.ToLower(sb[i]) == Char.ToLower(value[0]))
                    {
                        index = 1;
                        while ((index < length) && (Char.ToLower(sb[i + index]) == Char.ToLower(value[index])))
                            ++index;

                        if (index == length)
                            return i;
                    }
                }

                return -1;
            }

            for (int i = startIndex; i < maxSearchLength; ++i)
            {
                if (sb[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (sb[i + index] == value[index]))
                        ++index;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 2)
                    throw new Exception("Error: Incorrect number of arguments expected 2");

                string pathToManifest = null;
                var pathToProject = Path.GetFullPath(args[0]).Replace("\\", "/");
                if (Directory.Exists(pathToProject))
                {
                    var path = Path.Combine(pathToProject, "Packages");
                    if (Directory.Exists(path))
                    {
                        pathToManifest = Path.Combine(path, "manifest.json");
                    }
                }
                else if (File.Exists(pathToProject) && pathToProject.EndsWith(".json"))
                {
                    pathToManifest = pathToProject;
                    Console.WriteLine($"Manifest at path {pathToManifest} will be patched");
                }

                if (pathToManifest == null)
                    throw new Exception($"Error: Can not find manifest at path {pathToProject}");

                var pathToGraphicsRepo = Path.GetFullPath(args[1]).Replace("\\", "/");
                if (!Directory.Exists(pathToGraphicsRepo))
                    throw new Exception($"Error: Can not find graphics repo folder in {pathToGraphicsRepo}");

                PathManifest(pathToManifest, pathToGraphicsRepo);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.ToString()}");
            }
        }

        static void PathManifest(string pathToManifest, string pathToGraphicsRepo)
        {
            var manifestContent = File.ReadAllText(pathToManifest);
            var builder = new StringBuilder(manifestContent);

            PatchPackage(builder, "com.unity.render-pipelines.universal", pathToGraphicsRepo);
            PatchPackage(builder, "com.unity.shadergraph", pathToGraphicsRepo);
            PatchPackage(builder, "com.unity.render-pipelines.core", pathToGraphicsRepo);

            Console.WriteLine(builder.ToString());
            File.WriteAllText(pathToManifest, builder.ToString());
        }

        static void PatchPackage(StringBuilder builder, string packageName, string pathToGraphicsRepo)
        {
            if (ContainsPackage(builder, packageName))
            {
                ReplacePackage(builder, packageName, pathToGraphicsRepo);
            }
            else
            {
                InsertPackage(builder, packageName, pathToGraphicsRepo);
            }
        }

        static bool ContainsPackage(StringBuilder builder, string packageName)
        {
            return builder.IndexOf(packageName, 0, false) != -1;
        }

        static void ReplacePackage(StringBuilder builder, string packageName, string pathToGraphicsRepo)
        {
            var index = builder.IndexOf(packageName, 0, false);
            if (index == -1)  
                throw new Exception("Manifest unexpected structure");

            index = builder.IndexOf(":", index, false);
            if (index == -1)  
                throw new Exception("Manifest unexpected structure");

            var indexStart = builder.IndexOf("\"", index, false);
            if (indexStart == -1)  
                throw new Exception("Manifest unexpected structure");
            indexStart +=1;

            var indexEnd = builder.IndexOf("\"", indexStart, false);
            if (indexEnd == -1)  
                throw new Exception("Manifest unexpected structure");

            builder.Remove(indexStart, indexEnd - indexStart);

            var result = $"file:{pathToGraphicsRepo}/{packageName}";
            builder.Insert(indexStart, result);

            Console.WriteLine($"Package {packageName} patched into manifest!");
        }

        static void InsertPackage(StringBuilder builder, string packageName, string pathToGraphicsRepo)
        {
            var index = builder.IndexOf("\"dependencies\": {", 0, false);
            if (index == -1)  
                throw new Exception("Manifest unexpected structure");

            index = builder.IndexOf("\n", index, false);
            if (index == -1)  
                throw new Exception("Manifest unexpected structure");

            var indent = "    ";
            var result = $"\n{indent}\"{packageName}\": \"file:{pathToGraphicsRepo}/{packageName}\",";
            builder.Insert(index, result);

            Console.WriteLine($"Package {packageName} added into manifest!");
        }
    }
}
