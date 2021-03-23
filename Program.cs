using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using AssetStudio;

namespace atlastool
{
    class Program
    {
        static SHA1 hash = SHA1.Create();
        public static Bitmap CreateBitmapFromTexture2D(Texture2D texture)
        {
            var bitmap = new Bitmap(texture.m_Width, texture.m_Height, PixelFormat.Format32bppArgb);

            return bitmap;
        }

        public static void ExportAtlas(string inputPath, string outputpath)
        {
            Console.WriteLine($"Loading asset file {inputPath}");
            AssetsManager assetManager = new AssetsManager();
            Console.WriteLine(inputPath);

            if (File.Exists(inputPath))
            {
                assetManager.LoadFiles(new string[] { inputPath });
            }
            else if (Directory.Exists(inputPath))
            {
                assetManager.LoadFolder(inputPath);
            }
            else
            {
                Console.WriteLine("Error loading file");
                return;
            }

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
                            if (text.m_Name == "version")
                            {
                                gameVersion = System.Text.Encoding.UTF8.GetString(text.m_Script);
                            }
                            break;
                    }
                }
            }
            string gameversionOutputPath = Path.Combine(outputpath, gameVersion);

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

                                image.Save(filePath, ImageFormat.Png);
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
                    var bitmap = texture.ConvertToBitmap(false);
                    bitmap.Save(Path.Combine(gameversionOutputPath, $"{texture.m_Name}.png"), ImageFormat.Png);
                    Console.WriteLine($"Atlas texture2d {texture.m_Name} saved");
                    json.WriteStringValue($"{texture.m_Name}.png");
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
            List<(string fileName, string name, string hash)> imageData = new List<(string fileName, string name, string hash)>();
            List<string> atlasTextures = new List<string>();

            json.ReadArrayStart("imageData");

            while (json.Read() && json.TokenType != JsonTokenType.EndArray)
            {
                json.ReadObjectStart(skipRead: true);
                string filename = json.ReadString("fileName");
                string name = json.ReadString("name");
                string hash = json.ReadString("hash");
                json.ReadObjectEnd();
                imageData.Add((filename, name, hash));
            }
            json.ReadStringArray("spriteAtlas", atlasTextures);

            foreach ((string fileName, string originalName, string fileHash) in imageData)
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
                    Console.WriteLine($"Changed file detected! {fileName}");
                }
            }
                Console.WriteLine($"Finished loading data {atlasTextures[0]}");
        }

        static void Main(string[] args)
        {
            string input = string.Empty;
            string output = "";
            bool extract = false;
            bool combine = false;
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg == "--input" || arg == "-i")
                {
                    i++;
                    input = args[i].Trim();
                }
                else if (arg == "--output" || arg == "-o")
                {
                    i++;
                    output = args[i].Trim();
                }
                else if (arg == "--export" || arg == "-x")
                {
                    extract = true;
                }
                else if (arg == "--combine" || arg == "-c")
                {
                    combine = true;
                }
                else if (arg == "--help" || arg == "-h")
                {
                    Console.WriteLine($"Atlas Tool \n");
                    Console.WriteLine($"Usage [arguments]\n");
                    Console.WriteLine($"arguments:");
                    Console.WriteLine($"-i|--input \tFile or folder to extract from");
                    Console.WriteLine($"-o|--output \tOutput directory to extract into");
                    Console.WriteLine($"-x|--export \tExport sprites from sprite atlas");
                    Console.WriteLine($"-c|--combine \tCombine modified sprites into new atlas image");
                    return;
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
