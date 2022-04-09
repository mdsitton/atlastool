// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text.Json;
// using System.Threading.Tasks;
// using AssetStudio;
// using SixLabors.ImageSharp;
// using SixLabors.ImageSharp.PixelFormats;
// using SixLabors.ImageSharp.Processing;

// namespace atlascore;

// public class CombineAtlas
// {

//     public static void Combine(string inputPath)
//     {
//         var jsonText = File.ReadAllBytes(Path.Combine(inputPath, "spriteData.json"));

//         Utf8JsonReader json = new Utf8JsonReader(jsonText);
//         json.ReadObjectStart();
//         string gameVersion = json.ReadString("gameVersion");
//         Console.WriteLine($"Found data folder for game version {gameVersion}");

//         string spriteDir = Path.Combine(inputPath, $"sprites");

//         string assetDataPath = json.ReadString("unityAssetPath");

//         AssetsManager assetManager = AssetStudioUtil.LoadAssetManager(assetDataPath);
//         if (assetManager == null)
//         {
//             Console.WriteLine("Error: Could not load game data from stored data path.");
//             return;
//         }

//         List<(string fileName, string name, string hash, int pathID)> imageData = new List<(string fileName, string name, string hash, int pathID)>();
//         List<string> atlasTextures = new List<string>();

//         json.ReadArrayStart("imageData");

//         while (json.Read() && json.TokenType != JsonTokenType.EndArray)
//         {
//             json.ReadObjectStart(skipRead: true);
//             string filename = json.ReadString("fileName");
//             string name = json.ReadString("name");
//             string hash = json.ReadString("hash");
//             int pathID = json.ReadInt32("pathID");
//             json.ReadObjectEnd();
//             imageData.Add((filename, name, hash, pathID));
//         }
//         json.ReadStringArray("spriteAtlas", atlasTextures);

//         Dictionary<string, Image<Bgra32>> atlases = new Dictionary<string, Image<Bgra32>>();

//         foreach (var atlas in atlasTextures)
//         {
//             var img = Image.Load<Bgra32>(Path.Combine(inputPath, $"{atlas}.png"));

//             atlases.Add(atlas, img);
//             atlases[atlas].Mutate((x) => x.Flip(FlipMode.Vertical));
//         }

//         foreach ((string fileName, string originalName, string fileHash, int pathID) in imageData)
//         {
//             string filePath = Path.Combine(spriteDir, fileName);

//             //image.Save(filePath, ImageFormat.Png);
//             // byte[] hashData;

//             // using (var f = File.OpenRead(filePath))
//             // {
//             //     if (hash == null)
//             //     {
//             //         hash = SHA1.Create();
//             //     }

//             //     hashData = hash.ComputeHash(f);
//             // }
//             // string hashString = Convert.ToBase64String(hashData);
//             // if (hashString != fileHash)
//             // {
//             foreach (var assetFile in assetManager.assetsFileList)
//             {
//                 foreach (var obj in assetFile.Objects)
//                 {
//                     if (obj is Sprite spr && spr.m_Name == originalName && spr.m_PathID == pathID)
//                     {
//                         Console.WriteLine($"Changed file detected! {fileName}");
//                         var textureName = AssetStudioUtil.GetTextureName(spr);
//                         var atlas = atlases[textureName];

//                         var img = Image.Load<Bgra32>(filePath);

//                         ImageOps.ReplaceImage(atlas, img, spr);
//                     }
//                 }
//             }
//             // }
//         }

//         foreach (var atlas in atlasTextures)
//         {
//             var atlasImage = atlases[atlas];
//             atlasImage.Mutate((x) => x.Flip(FlipMode.Vertical));
//             atlasImage.SaveAsPng(Path.Combine(inputPath, $"{atlas}-changed.png"));
//             Console.WriteLine("Changed atlas written!");
//         }
//     }
// }