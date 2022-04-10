using System;
using System.IO;
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
            sprite.Texture = ImageOps.CutImage(texture.Texture, sprite);
        }
    }

    public static void MergeSprites(AtlasData atlasData)
    {
        foreach (var sprite in atlasData.Sprites)
        {
            var texture = atlasData.Textures[sprite.SourceTexturePathID];
            ImageOps.ReplaceImage(texture.Texture, sprite);
        }
    }

    public static void SaveTextures(AtlasData atlasData, string outputDir)
    {
        foreach (var tex in atlasData.Textures.Values)
        {
            var imgPath = Path.Combine(outputDir, $"{tex.Name}-{tex.PathID}.png");
            try
            {
                var texOut = tex.Texture.Clone((x) => x.Flip(FlipMode.Vertical));
                texOut.SaveAsPng(imgPath);
            }
            catch
            {
                Console.WriteLine($"error saving texture: {tex.Name}-{tex.PathID}.png");
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

        foreach (var sprite in atlasData.Sprites)
        {
            var spriteName = Path.Combine(texfolderPath, $"{sprite.Name}-{sprite.PathID}.png");
            sprite.Texture = Image.Load<Bgra32>(File.ReadAllBytes(spriteName));
        }

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
                if (sprite.TryGetSpriteTexture(out var texture))
                {
                    AssetStudio.Texture2D tex = texture!;
                    if (!atlasData.Textures.ContainsKey((int)tex.m_PathID))
                    {
                        var img = AssetStudio.Texture2DExtensions.ConvertToImage(tex, false);
                        var textureData = new TextureData(tex.m_Name, (int)tex.m_PathID, img);
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
        SaveTextures(atlasData, inputFolder);
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

        var jsonPath = Path.Combine(outputDir, $"{atlasData.Name}.json");
        AtlasData.SerializeToFile(atlasData, jsonPath);
        SaveTextures(atlasData, outputDir);
        foreach (var sprite in atlasData.Sprites)
        {
            var imgPath = Path.Combine(outputDir, atlasData.Name, $"{sprite.Name}-{sprite.PathID}.png");
            try
            {
                sprite.Texture!.SaveAsPng(imgPath);
            }
            catch
            {
                Console.WriteLine($"error saving texture: {sprite.Name}-{sprite.PathID}.png");
                continue;
            }
        }
    }

}