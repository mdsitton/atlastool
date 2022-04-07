using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using AssetStudio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace atlastool
{
    class Program
    {
        static SHA1 hash = SHA1.Create();

        public static AssetsManager LoadAssetManager(string path)
        {
            AssetsManager assetManager = new AssetsManager();

            if (File.Exists(path))
            {
                assetManager.LoadFiles(new string[] { path });
            }
            else if (Directory.Exists(path))
            {
                assetManager.LoadFolder(path);
            }
            else
            {
                Console.WriteLine("Error loading file");
                return null;
            }
            return assetManager;
        }

        public static void ExportAtlas(string inputPath, string outputPath)
        {
            Console.WriteLine($"Loading assets from {inputPath}");
            AssetsManager assetManager = LoadAssetManager(inputPath);

            List<Texture2D> atlasTextures = new List<Texture2D>();
            string gameVersion = string.Empty;


            // find game version first
            foreach (var file in assetManager.assetsFileList)
            {
                foreach (var obj in file.Objects)
                {
                    switch (obj)
                    {
                        case TextAsset text:
                            {
                                if (text.m_Name == "version")
                                {
                                    gameVersion = System.Text.Encoding.UTF8.GetString(text.m_Script);
                                }
                                break;
                            }
                    }
                }
            }
            string gameversionOutputPath = Path.Combine(outputPath, gameVersion);

            if (!Directory.Exists(gameversionOutputPath))
            {
                Directory.CreateDirectory(gameversionOutputPath);
            }

            string jsonOutputPath = Path.Combine(gameversionOutputPath, $"spriteData.json");
            string spriteOutputDir = Path.Combine(gameversionOutputPath, $"sprites");
            if (!Directory.Exists(spriteOutputDir))
            {
                Directory.CreateDirectory(spriteOutputDir);
            }
            if (File.Exists(jsonOutputPath))
            {
                File.Delete(jsonOutputPath);
            }
            using (var fs = File.OpenWrite(jsonOutputPath))
            using (var json = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
            {

                json.WriteStartObject();
                json.WriteString("gameVersion", gameVersion);
                json.WriteString("unityAssetPath", inputPath);
                json.WriteStartArray("imageData");
                foreach (var file in assetManager.assetsFileList)
                {
                    foreach (var obj in file.Objects)
                    {
                        switch (obj)
                        {
                            case Sprite spr:
                                bool foundAtlas = false;
                                if (spr.m_SpriteAtlas.TryGet(out var atlas))
                                {
                                    if (atlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var texture))
                                    {

                                        Console.WriteLine($"Found atlas texture {texture.m_Name}");
                                        if (!atlasTextures.Contains(texture))
                                        {
                                            atlasTextures.Add(texture);
                                        }
                                    }
                                    if (atlas.m_Name == "fiveFretAtlas")
                                    {
                                        foundAtlas = true;
                                    }
                                }

                                if (!foundAtlas)
                                    continue;

                                var image = SpriteHelper.GetImage(spr);
                                string filename = $"{spr.m_Name}_{spr.m_PathID}.png";
                                string filePath = Path.Combine(spriteOutputDir, filename);

                                image.SaveAsPng(filePath);
                                byte[] hashData;

                                using (var f = File.OpenRead(filePath))
                                {
                                    hashData = hash.ComputeHash(f);
                                }
                                string hashString = Convert.ToBase64String(hashData);

                                json.WriteStartObject();
                                json.WriteString("fileName", filename);
                                json.WriteString("name", spr.m_Name);
                                json.WriteString("hash", hashString);
                                json.WriteNumber("pathID", (int)spr.m_PathID);
                                json.WriteEndObject();

                                Console.WriteLine($"fiveFretAtlas Sprite {spr.m_Name} saved");
                                break;
                            case SpriteAtlas spriteAtlas:
                                Console.WriteLine($"Atlas {spriteAtlas.m_Name}");
                                break;
                        }
                    }
                }
                json.WriteEndArray();

                json.WriteStartArray("spriteAtlas");
                foreach (var texture in atlasTextures)
                {
                    var imageSavePath = Path.Combine(gameversionOutputPath, $"{texture.m_Name}.png");
                    var image = texture.ConvertToImage(true);
                    image.SaveAsPng(imageSavePath);
                    Console.WriteLine($"Atlas texture2d {texture.m_Name} saved");
                    json.WriteStringValue(texture.m_Name);
                }
                json.WriteEndArray();

                json.WriteEndObject();
            }
        }


        public static void CombineAtlas(string inputPath)
        {
            var jsonText = File.ReadAllBytes(Path.Combine(inputPath, "spriteData.json"));

            Utf8JsonReader json = new Utf8JsonReader(jsonText);
            json.ReadObjectStart();
            string gameVersion = json.ReadString("gameVersion");
            Console.WriteLine($"Found data folder for game version: {gameVersion}");

            string spriteDir = Path.Combine(inputPath, $"sprites");

            string assetDataPath = json.ReadString("unityAssetPath");

            AssetsManager assetManager = LoadAssetManager(assetDataPath);
            List<(string fileName, string name, string hash, int pathID)> imageData = new List<(string fileName, string name, string hash, int pathID)>();
            List<string> atlasTextures = new List<string>();

            json.ReadArrayStart("imageData");

            while (json.Read() && json.TokenType != JsonTokenType.EndArray)
            {
                json.ReadObjectStart(skipRead: true);
                string filename = json.ReadString("fileName");
                string name = json.ReadString("name");
                string hash = json.ReadString("hash");
                int pathID = json.ReadInt32("pathID");
                json.ReadObjectEnd();
                imageData.Add((filename, name, hash, pathID));
            }
            json.ReadStringArray("spriteAtlas", atlasTextures);

            Dictionary<string, Image<Bgra32>> atlases = new Dictionary<string, Image<Bgra32>>();
            Dictionary<string, List<BitmapUpdate.SwapData>> swaps = new Dictionary<string, List<BitmapUpdate.SwapData>>();

            foreach (var atlas in atlasTextures)
            {
                var img = Image.Load<Bgra32>(Path.Combine(inputPath, $"{atlas}.png"));

                atlases.Add(atlas, img);
                swaps.Add(atlas, new List<BitmapUpdate.SwapData>());
                atlases[atlas].Mutate((x) => x.Flip(FlipMode.Vertical));
            }

            foreach ((string fileName, string originalName, string fileHash, int pathID) in imageData)
            {

                string filePath = Path.Combine(spriteDir, fileName);

                //image.Save(filePath, ImageFormat.Png);
                byte[] hashData;

                using (var f = File.OpenRead(filePath))
                {
                    hashData = hash.ComputeHash(f);
                }
                string hashString = Convert.ToBase64String(hashData);
                if (hashString != fileHash)
                {
                    foreach (var assetFile in assetManager.assetsFileList)
                    {
                        foreach (var obj in assetFile.Objects)
                        {
                            if (obj is Sprite spr && spr.m_Name == originalName && spr.m_PathID == pathID)
                            {
                                Console.WriteLine($"Changed file detected! {fileName}");
                                var textureName = BitmapUpdate.GetTextureName(spr);
                                var atlas = atlases[textureName];

                                var img = Image.Load<Bgra32>(filePath);

                                var data = BitmapUpdate.PrepareSwapData(atlas, img, spr);
                                if (data != null)
                                {
                                    swaps[textureName].Add(data);
                                }
                                else
                                {
                                    Console.WriteLine($"Error: Failed to replace image. File name or image size incorrect {fileName}");
                                }
                            }
                        }
                    }
                }
            }

            foreach (var atlas in atlasTextures)
            {
                var atlasImage = atlases[atlas];
                BitmapUpdate.ProcessSwaps(atlasImage, swaps[atlas].ToArray());
                atlasImage.Mutate((x) => x.Flip(FlipMode.Vertical));
                atlasImage.SaveAsPng(Path.Combine(inputPath, $"{atlas}-changed.png"));
                Console.WriteLine("Changed atlas written!");
            }

        }

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
                                Console.WriteLine($"Error: The specified input path does not exist.");
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
                                Console.WriteLine($"Error: The specified output path does not exist.");
                                return;
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
                            Console.WriteLine("                            \tMust be followed by the -i and (optionally) -o argument.");
                            Console.WriteLine("-c        | --combine       \tCombine modified sprites into a new atlas image.");
                            Console.WriteLine("                            \tMust be followed by the -i argument.");
                            Console.WriteLine("-i <path> | --input <path>  \tThe input file or folder to extract/combine from.");
                            Console.WriteLine("                            \tFor extracting, it can be either the data.unity3d file, resources.assets, or Clone Hero_Data folder.");
                            Console.WriteLine("                            \tFor combining, it should be the folder you want to combine sprites from.");
                            Console.WriteLine("-o <path> | --output <path> \tThe output directory to extract to.");
                            Console.WriteLine("                            \tIf unspecified, defaults to atlastool's own folder.");
                            Console.WriteLine();
                            Console.WriteLine("Examples:");
                            Console.WriteLine("  Extracting:");
                            Console.WriteLine(@"    -x -i C:\Games\Clone Hero\Clone Hero_Data\unity.data3d -o .\extracted");
                            Console.WriteLine(@"    -x -i %APPDATA%\Clone Hero Launcher\gameFiles\Clone Hero_Data");
                            Console.WriteLine();
                            Console.WriteLine("  Combining:");
                            Console.WriteLine(@"    -c -i .\v.23.2.2");
                            return;
                        }
                }
            }

            if (extract && combine)
            {
                Console.WriteLine("Error: Cannot extract and combine at the same time");
                return;
            }
            if (extract && input != string.Empty)
            {
                ExportAtlas(input, output);
            }
            else if (extract)
            {
                Console.WriteLine("Error: Export needs an input and output path");
            }

            if (combine && input != string.Empty)
            {
                CombineAtlas(input);
            }
            else if (combine)
            {
                Console.WriteLine("Error: Combine needs an input path");
            }
        }
    }
}
