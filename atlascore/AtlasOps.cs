using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace atlascore;

public class AtlasOps
{
    public static void SliceSprites(AtlasData atlasData)
    {
        foreach (var sprite in atlasData.Sprites)
        {
            var texture = atlasData.Textures[sprite.SourceTexturePathID];
            if (texture.Texture != null)
                sprite.Texture = ImageOps.CutImage(texture.Texture, sprite);
        }
    }

    public static void MergeSprites(AtlasData atlasData)
    {
        foreach (var sprite in atlasData.Sprites)
        {
            if (!sprite.isChanged)
                continue;
            var texture = atlasData.Textures[sprite.SourceTexturePathID];
            if (texture.Texture != null)
            {
                ImageOps.ReplaceImage(texture.Texture, sprite);
                texture.isChanged = true;
            }
        }
    }

    public static void SaveTextures(AtlasData atlasData, string outputDir)
    {
        foreach (var tex in atlasData.Textures.Values)
        {
            string name = $"{tex.Name}-{tex.PathID}";
            if (tex.isChanged)
            {
                name += "-changed";
            }
            name += ".png";
            var imgPath = Path.Combine(outputDir, name);
            try
            {
                var texOut = tex.Texture.Clone((x) => x.Flip(FlipMode.Vertical));
                texOut.SaveAsPng(imgPath);
            }
            catch
            {
                Console.WriteLine($"error saving texture: ${name}");
                continue;
            }
        }
    }

    public static void LoadTextures(AtlasData atlasData, string outputDir)
    {
        foreach (var tex in atlasData.Textures.Values)
        {
            var imgPath = Path.Combine(outputDir, $"{tex.Name}-{tex.PathID}.png");
            try
            {
                tex.Texture = Image.Load<Bgra32>(imgPath);
                // in memory should be flipped version of texture
                tex.Texture.Mutate((x) => x.Flip(FlipMode.Vertical));
            }
            catch
            {
                Console.WriteLine($"error loading texture: {tex.Name}-{tex.PathID}.png");
            }
        }
    }

    public static AtlasData? ReadAtlasDataFromJson(string jsonPath)
    {
        var folderPath = Path.GetDirectoryName(jsonPath) ?? "";
        var texfolderPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(jsonPath));
        var atlasData = AtlasData.DeserializeFromFile(jsonPath);

        if (atlasData == null)
            return null;


        LoadTextures(atlasData, folderPath);

        byte[]?[] filesData = new byte[]?[atlasData.Sprites.Count];

        for (int i = 0; i < atlasData.Sprites.Count; ++i)
        {
            var sprite = atlasData.Sprites[i];
            var spriteName = Path.Combine(texfolderPath, $"{sprite.Name}-{sprite.PathID}.png");
            filesData[i] = File.ReadAllBytes(spriteName);
        }

        var hashes = HashSpriteFiles(filesData);

        Parallel.For(0, filesData.Length, (i) =>
        {
            var sprite = atlasData.Sprites[i];
            if (sprite.InitialFileHash != hashes[i])
            {
                sprite.isChanged = true;
                Console.WriteLine($"Modified sprite found:  {sprite.Name}-{sprite.PathID}.png");
            }
            sprite.Texture = Image.Load<Bgra32>(filesData[i]);
        });

        return atlasData;
    }

    public static AtlasData? ReadAtlasDataFromAssets(string assetPath)
    {
        AssetStudio.AssetsManager assetManager = AssetStudioUtil.LoadAssetManager(assetPath);
        if (assetManager == null)
        {
            Console.WriteLine("Error: Could not load game data. Please ensure the input path is for the game's data folder or data.unity3d.");
            return null;
        }

        var gameVersion = AssetStudioUtil.GetGameVersion(assetManager.assetsFileList);


        foreach (var atlas in assetManager.assetsFileList.EnumerateAssets<AssetStudio.SpriteAtlas>())
        {
            // Limit to just the fiveFretAtlas for now
            if (atlas.m_Name != "fiveFretAtlas")
                continue;

            AtlasData atlasData = new(gameVersion, assetPath, atlas.m_Name, (int)atlas.m_PathID);
            foreach (var sprite in atlas.EnumerateAtlasSprites())
            {
                if (sprite.TryGetSpriteTexture(out var texture, out int fileID))
                {
                    AssetStudio.Texture2D tex = texture!;
                    if (!atlasData.Textures.ContainsKey((int)tex.m_PathID))
                    {
                        var img = AssetStudio.Texture2DExtensions.ConvertToImage(tex, false);
                        var textureData = new TextureData(tex.m_Name, fileID, (int)tex.m_PathID, img);
                        atlasData.Textures[(int)tex.m_PathID] = textureData;
                    }

                    var spriteData = new SpriteData(
                        (int)sprite.m_PathID,
                        sprite.m_Name,
                        atlas.GetOrientation(sprite),
                        atlas.GetRectData(sprite),
                        (int)tex.m_PathID);

                    atlasData.Sprites.Add(spriteData);
                }
            }
            return atlasData;
        }
        return null;
    }

    public static void CombineFromPath(string inputFolder)
    {

        var atlasData = AtlasOps.ReadAtlasDataFromJson(Path.Combine(inputFolder, $"fiveFretAtlas.json"));

        if (atlasData == null)
            return;

        MergeSprites(atlasData);

        bool foundChanged = false;

        foreach (var tex in atlasData.Textures.Values)
        {
            if (tex.isChanged)
            {
                foundChanged = true;
            }
        }

        if (foundChanged)
        {
            SaveTextures(atlasData, inputFolder);
        }
        else
        {
            Console.WriteLine("No changed textures found!");
        }
    }

    [ThreadStatic]
    public static SHA1 hash = SHA1.Create();

    public static string[] HashSpriteFiles(byte[]?[] filesData)
    {
        string[] newHashes = new string[filesData.Length];
        Parallel.For(0, filesData.Length, (i) =>
        {
            if (hash == null)
            {
                hash = SHA1.Create();
            }

            var data = filesData[i];
            if (data != null)
            {
                newHashes[i] = Convert.ToBase64String(hash.ComputeHash(data));
            }
        });
        return newHashes;
    }

    public static void ExtractToFolder(string input, string output)
    {
        Console.WriteLine($"Loading assets from {input}");

        var atlasData = ReadAtlasDataFromAssets(input);

        if (atlasData == null)
            return;

        SliceSprites(atlasData);

        var outputDir = Path.Combine(output, atlasData.GameVersion);
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, atlasData.Name));

        SaveTextures(atlasData, outputDir);
        byte[]?[] filesData = new byte[]?[atlasData.Sprites.Count];

        Parallel.For(0, filesData.Length, (i) =>
        {
            var sprite = atlasData.Sprites[i];
            try
            {
                using (var ms = new MemoryStream())
                {
                    sprite.Texture!.SaveAsPng(ms);
                    filesData[i] = ms.GetBuffer();
                }
            }
            catch
            {
                filesData[i] = null;
                Console.WriteLine($"error saving texture: {sprite.Name}-{sprite.PathID}.png");
                return;
            }
        });

        var fileHashes = HashSpriteFiles(filesData);

        for (int i = 0; i < atlasData.Sprites.Count; ++i)
        {
            var sprite = atlasData.Sprites[i];
            sprite.InitialFileHash = fileHashes[i];
        }

        var jsonPath = Path.Combine(outputDir, $"{atlasData.Name}.json");
        AtlasData.SerializeToFile(atlasData, jsonPath);

        for (int i = 0; i < atlasData.Sprites.Count; ++i)
        {
            var sprite = atlasData.Sprites[i];
            var data = filesData[i];
            if (data != null)
            {
                var imgPath = Path.Combine(outputDir, atlasData.Name, $"{sprite.Name}-{sprite.PathID}.png");
                File.WriteAllBytes(imgPath, data);
            }
            else
            {
                Console.WriteLine($"error saving texture: {sprite.Name}-{sprite.PathID}.png");
            }
        }
    }

}