﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        [ThreadStatic] static SHA1 hash;

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
                return null;
            }
            return assetManager;
        }

        public static string GetGameVersion(List<SerializedFile> assetList)
        {
            foreach (var file in assetList)
            {
                foreach (var obj in file.Objects)
                {
                    switch (obj)
                    {
                        case TextAsset text:
                            if (text.m_Name == "version")
                            {
                                return System.Text.Encoding.UTF8.GetString(text.m_Script);
                            }
                            break;
                    }
                }
            }
            return String.Empty;
        }

        public class SpriteJsonData
        {
            public string fileName;
            public int pathID;
            public string hash;
            public string name;
        }

        private static string jsonOutputPath;
        private static string spriteOutputDir;
        private static string gameVersion;
        private static string inputPath;
        private static string gameversionOutputPath;

        public static void StartExtract(string input, string output)
        {


            Console.WriteLine($"Loading assets from {input}");
            inputPath = input;
            AssetsManager assetManager = LoadAssetManager(input);
            if (assetManager == null)
            {
                Console.WriteLine("Error: Could not load game data. Please ensure the input path is for the game's data folder or data.unity3d.");
                return;
            }

            SetupPaths(assetManager, output);
            ExportAtlas(assetManager);
        }

        public static void SetupPaths(AssetsManager assetsManager, string outputPath)
        {

            // find game version first
            gameVersion = GetGameVersion(assetsManager.assetsFileList);

            gameversionOutputPath = Path.Combine(outputPath, gameVersion);

            if (!Directory.Exists(gameversionOutputPath))
            {
                Directory.CreateDirectory(gameversionOutputPath);
            }

            jsonOutputPath = Path.Combine(gameversionOutputPath, $"spriteData.json");
            spriteOutputDir = Path.Combine(gameversionOutputPath, $"sprites");
            if (!Directory.Exists(spriteOutputDir))
            {
                Directory.CreateDirectory(spriteOutputDir);
            }
            if (File.Exists(jsonOutputPath))
            {
                File.Delete(jsonOutputPath);
            }
        }

        public static void WriteSpriteJsonData(ConcurrentBag<SpriteJsonData> spriteData, List<Texture2D> atlasTextures)
        {

            using (var fs = File.OpenWrite(jsonOutputPath))
            using (var json = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
            {

                json.WriteStartObject();
                json.WriteString("gameVersion", gameVersion);
                json.WriteString("unityAssetPath", inputPath);
                json.WriteStartArray("imageData");

                foreach (var data in spriteData)
                {

                    json.WriteStartObject();
                    json.WriteString("fileName", data.fileName);
                    json.WriteString("name", data.name);
                    json.WriteString("hash", data.hash);
                    json.WriteNumber("pathID", data.pathID);
                    json.WriteEndObject();
                }
                json.WriteEndArray();
                json.WriteStartArray("spriteAtlas");
                foreach (var texture in atlasTextures)
                {
                    json.WriteStringValue(texture.m_Name);
                }
                json.WriteEndArray();

                json.WriteEndObject();

            }
        }

        public static void ExportAtlas(AssetsManager assetManager)
        {

            List<Texture2D> atlasTextures = new List<Texture2D>();

            Image<Bgra32> atlasImage = null;

            List<Sprite> foundSprites = new List<Sprite>();
            foreach (var file in assetManager.assetsFileList)
            {
                foreach (var obj in file.Objects)
                {
                    if (obj is Sprite spr)
                    {
                        if (spr.m_SpriteAtlas.TryGet(out var atlas))
                        {
                            if (atlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var texture))
                            {

                                if (!atlasTextures.Contains(texture))
                                {
                                    Console.WriteLine($"Found atlas texture {texture.m_Name}");
                                    atlasTextures.Add(texture);
                                    atlasImage = texture.ConvertToImage(false);
                                }
                            }
                            if (atlas.m_Name == "fiveFretAtlas")
                            {
                                foundSprites.Add(spr);
                                Console.WriteLine($"Queued atlas sprite {spr.m_Name}");
                            }
                        }
                    }
                }
            }

            using (atlasImage)
            {
                ConcurrentBag<SpriteJsonData> jsonData = new ConcurrentBag<SpriteJsonData>();
                Parallel.ForEach(foundSprites, spr =>
                {
                    if (spr.m_SpriteAtlas.TryGet(out var atlas))
                    {
                        if (atlas.m_RenderDataMap.TryGetValue(spr.m_RenderDataKey, out var atlasData))
                        {

                            var image = SpriteHelper.CutImage(spr, atlasImage, atlasData.textureRect, atlasData.textureRectOffset, atlasData.downscaleMultiplier, atlasData.settingsRaw);
                            string filename = $"{spr.m_Name}_{spr.m_PathID}.png";
                            string filePath = Path.Combine(spriteOutputDir, filename);
                            string hashString = string.Empty;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                image.SaveAsPng(ms);
                                ms.Flush();
                                ms.Seek(0, SeekOrigin.Begin);

                                if (hash == null)
                                {
                                    hash = SHA1.Create();
                                }

                                byte[] hashData = hash.ComputeHash(ms);
                                hashString = Convert.ToBase64String(hashData);
                                using (var os = File.OpenWrite(filePath))
                                {
                                    ms.WriteTo(os);
                                }
                                hashString = Convert.ToBase64String(hashData);

                                var spriteData = new SpriteJsonData
                                {
                                    fileName = filename,
                                    name = spr.m_Name,
                                    pathID = (int)spr.m_PathID,
                                    hash = hashString
                                };
                                jsonData.Add(spriteData);
                            }
                            Console.WriteLine($"Saved atlas sprite {spr.m_Name}");
                        }
                    }
                });
                foreach (var texture in atlasTextures)
                {
                    var imageSavePath = Path.Combine(gameversionOutputPath, $"{texture.m_Name}.png");
                    // var image = texture.ConvertToImage(true);
                    atlasImage.Mutate((x) => x.Flip(FlipMode.Vertical));
                    atlasImage.SaveAsPng(imageSavePath);
                    Console.WriteLine($"Saved atlas {texture.m_Name}");
                }
                WriteSpriteJsonData(jsonData, atlasTextures);
            }
        }

        public static void CombineAtlas(string inputPath)
        {
            var jsonText = File.ReadAllBytes(Path.Combine(inputPath, "spriteData.json"));

            Utf8JsonReader json = new Utf8JsonReader(jsonText);
            json.ReadObjectStart();
            string gameVersion = json.ReadString("gameVersion");
            Console.WriteLine($"Found data folder for game version {gameVersion}");

            string spriteDir = Path.Combine(inputPath, $"sprites");

            string assetDataPath = json.ReadString("unityAssetPath");

            AssetsManager assetManager = LoadAssetManager(assetDataPath);
            if (assetManager == null)
            {
                Console.WriteLine("Error: Could not load game data from stored data path.");
                return;
            }

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

            foreach (var atlas in atlasTextures)
            {
                var img = Image.Load<Bgra32>(Path.Combine(inputPath, $"{atlas}.png"));

                atlases.Add(atlas, img);
                atlases[atlas].Mutate((x) => x.Flip(FlipMode.Vertical));
            }

            foreach ((string fileName, string originalName, string fileHash, int pathID) in imageData)
            {

                string filePath = Path.Combine(spriteDir, fileName);

                //image.Save(filePath, ImageFormat.Png);
                byte[] hashData;

                using (var f = File.OpenRead(filePath))
                {
                    if (hash == null)
                    {
                        hash = SHA1.Create();
                    }

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

                                BitmapUpdate.ProcessChanges(atlas, img, spr);
                            }
                        }
                    }
                }
            }

            foreach (var atlas in atlasTextures)
            {
                var atlasImage = atlases[atlas];
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
                StartExtract(input, output);
            }
            else if (extract)
            {
                Console.WriteLine("Error: Exporting requires an input path to the game's data folder or unity.data3d. Use the -i parameter to specify the path.");
            }

            if (combine && input != string.Empty)
            {
                CombineAtlas(input);
            }
            else if (combine)
            {
                Console.WriteLine("Error: Combining requires an input path to the folder with the sprites to combine. Use the -i parameter to specify the path.");
            }
        }
    }
}
