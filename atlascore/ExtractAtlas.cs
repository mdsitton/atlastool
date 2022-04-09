using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AssetStudio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace atlascore;

public static class ExtractAtlas
{

    public static void StartExtract(string input, string output)
    {
        Console.WriteLine($"Loading assets from {input}");

        var atlasData = ReadAtlasData(input);

        if (atlasData == null)
            return;

        AtlasOps.SliceSprites(atlasData);

        var outputDir = Path.Combine(output, atlasData.GameVerion);
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, atlasData.Name));

        var jsonPath = Path.Combine(outputDir, $"{atlasData.Name}.json");
        AtlasData.SerializeToFile(atlasData, jsonPath);
        AtlasOps.SaveTextures(atlasData, outputDir);
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
        // AtlasOps.MergeSprites(atlasData);
        // AtlasOps.SaveTextures(atlasData, outputDir);
    }

    public static AtlasData? ReadAtlasData(string assetPath)
    {
        AssetsManager assetManager = AssetStudioUtil.LoadAssetManager(assetPath);
        if (assetManager == null)
        {
            Console.WriteLine("Error: Could not load game data. Please ensure the input path is for the game's data folder or data.unity3d.");
            return null;
        }

        var gameVersion = AssetStudioUtil.GetGameVersion(assetManager.assetsFileList);


        foreach (var atlas in assetManager.assetsFileList.EnumerateAssets<SpriteAtlas>())
        {
            // Limit to just the fiveFretAtlas for now
            if (atlas.m_Name != "fiveFretAtlas")
                continue;

            AtlasData atlasData = new(gameVersion, assetPath, atlas.m_Name, (int)atlas.m_PathID);
            foreach (var sprite in atlas.EnumerateAtlasSprites())
            {
                if (sprite.TryGetSpriteTexture(out var texture))
                {
                    Texture2D tex = texture!;
                    if (!atlasData.Textures.ContainsKey((int)tex.m_PathID))
                    {
                        var img = tex.ConvertToImage(false);
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
}