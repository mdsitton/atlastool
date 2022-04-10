using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using atlascore;

namespace atlastool
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = string.Empty;
            string output = string.Empty;
            bool extract = false;
            bool combine = false;
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--input":
                    case "-i":
                        {
                            i++;
                            input = args[i].Trim();
                            if (!Directory.Exists(input))
                            {
                                Console.WriteLine("Error: The specified input path does not exist. Please ensure it has been typed correctly (use quotes if it has spaces).");
                                return;
                            }
                            break;
                        }

                    case "--output":
                    case "-o":
                        {
                            i++;
                            output = args[i].Trim();
                            if (!Directory.Exists(output))
                            {
                                try
                                {
                                    Directory.CreateDirectory(output);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error: Could not create output directory.");
                                    Console.WriteLine($"Exception info: {ex.Message}");
                                    return;
                                }
                            }
                            break;
                        }

                    case "--export":
                    case "-x":
                        {
                            extract = true;
                            break;
                        }

                    case "--combine":
                    case "-c":
                        {
                            combine = true;
                            break;
                        }

                    case "--help":
                    case "-h":
                        {
                            Console.WriteLine("Usage:");
                            Console.WriteLine("-h        | --help          \tDisplay information about available commands.");
                            Console.WriteLine("-x        | --export        \tExport the sprite atlas and individual sprites.");
                            Console.WriteLine("                            \tMust be followed by the -i and (optionally) -o arguments.");
                            Console.WriteLine("-c        | --combine       \tCombine modified sprites into a new atlas image.");
                            Console.WriteLine("                            \tMust be followed by the -i argument.");
                            Console.WriteLine("-i <path> | --input <path>  \tThe input file or folder to extract/combine from.");
                            Console.WriteLine("                            \tFor extracting, it can be either the data.unity3d file, resources.assets, or Clone Hero_Data folder.");
                            Console.WriteLine("                            \tFor combining, it should be the folder you want to combine sprites from.");
                            Console.WriteLine("-o <path> | --output <path> \tThe output directory to extract to.");
                            Console.WriteLine("                            \tIf unspecified, defaults to atlastool's own folder.");
                            Console.WriteLine();
                            Console.WriteLine("Examples:");
                            Console.WriteLine("- Extracting:");
                            Console.WriteLine(@"    -x -i C:\Games\Clone Hero\Clone Hero_Data\unity.data3d -o .\extracted");
                            Console.WriteLine(@"    -x -i %APPDATA%\Clone Hero Launcher\gameFiles\Clone Hero_Data");
                            Console.WriteLine();
                            Console.WriteLine("- Combining:");
                            Console.WriteLine(@"    -c -i .\v.23.2.2");
                            return;
                        }
                }
            }

            if (extract && combine)
            {
                Console.WriteLine("Error: Cannot extract and combine at the same time.");
                return;
            }

            if (extract && input != string.Empty)
            {
                AtlasOps.ExtractToFolder(input, output);
            }
            else if (extract)
            {
                Console.WriteLine("Error: Exporting requires an input path to the game's data folder or unity.data3d. Use the -i parameter to specify the path.");
            }

            if (combine && input != string.Empty)
            {
                AtlasOps.CombineFromPath(input);
            }
            else if (combine)
            {
                Console.WriteLine("Error: Combining requires an input path to the folder with the sprites to combine. Use the -i parameter to specify the path.");
            }
        }
    }
}
