using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace P5XBundleDeobfuscate
{
    class Program
    {
        public static List<string> errorList = new List<string>();
        public static uint GetHashCode(string content)
        {
            uint num = 131U;
            uint num2 = 0U;
            for (int i = 0; i < content.Length; i++)
            {
                num2 = num2 * num + content[i];
            }
            return num2 & 2147483647U;
        }

        public static int GetBundleObuscateOffset(string bundleName)
        {
            return (int)(GetHashCode(bundleName) % 32U + 8U);
        }


        public static async Task Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            Console.WriteLine("Persona 5X bundle deobfuscator\n");
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: drag and drop the folder where the bundles are located (subfolders are checked).\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            string[] filePaths = Directory.GetFiles(args[0], "*.bundle*", SearchOption.AllDirectories);

            List<Task> tasks = new List<Task>();
            foreach (string filePath in filePaths)
            {
                tasks.Add(Task.Run(() => ProcessFileAsync(filePath)));
            }

            await Task.WhenAll(tasks);

            timer.Stop();

            if (errorList.Count > 0)
            {
                Console.WriteLine("The following files encountered an error:");
                foreach (var error in errorList)
                {
                    Console.WriteLine(error);
                }
            }

            Console.WriteLine($"\nDone! Time elapsed: {timer.Elapsed}\nPress any key to exit...");
            Console.ReadKey();
        }

        public static async Task ProcessFileAsync(string filePath)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    int obfuscateOffset = GetBundleObuscateOffset(Path.GetFileName(filePath));

                    reader.BaseStream.Position = obfuscateOffset;

                    int Magic = reader.ReadInt32();

                    reader.BaseStream.Position -= 4;

                    if (Magic == 1953066581) // UnityFS Magic
                    {
                        byte[] remainingData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

                        using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                        {
                            await writer.BaseStream.WriteAsync(remainingData, 0, remainingData.Length);
                        }

                        Console.WriteLine($"Fixed and Saved file: {filePath}\n");
                    }
                    else
                    {
                        Console.WriteLine($"Duplicate UnityFS not found; skipping file {filePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                errorList.Add("Error on file: " + filePath + ". " + ex.Message);
            }
        }
    }
}
