using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace atlascore;

public class TextureData
{
    public string Name { get; set; }
    public int FileID { get; set; }
    public int PathID { get; set; }
    public Image<Bgra32>? Texture;

    [JsonConstructor]
    public TextureData(string name, int fileID, int pathID)
    {
        Name = name;
        FileID = fileID;
        PathID = pathID;
    }

    public TextureData(string name, int fileID, int pathID, Image<Bgra32> texture)
    {
        Name = name;
        FileID = fileID;
        PathID = pathID;
        Texture = texture;
    }
}

public class AtlasData
{
    public string GameVersion { get; set; }
    public string AssetPath { get; set; }
    public string Name { get; set; }
    public int PathID { get; set; }

    public Dictionary<int, TextureData> Textures { get; set; }
    public List<SpriteData> Sprites { get; set; }

    [JsonConstructor]
    public AtlasData(string gameVersion, string assetPath, string name, int pathID, Dictionary<int, TextureData> textures, List<SpriteData> sprites)
    {
        GameVersion = gameVersion;
        AssetPath = assetPath;
        Name = name;
        PathID = pathID;
        Textures = textures;
        Sprites = sprites;
    }

    public AtlasData(string gameVersion, string assetPath, string name, int pathID)
    {
        GameVersion = gameVersion;
        AssetPath = assetPath;
        Name = name;
        PathID = pathID;
        Textures = new Dictionary<int, TextureData>();
        Sprites = new List<SpriteData>();
    }

    public static void SerializeToFile(AtlasData atlasData, string fileName)
    {

        File.WriteAllBytes(fileName, JsonSerializer.SerializeToUtf8Bytes(atlasData, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static AtlasData? DeserializeFromFile(string fileName)
    {
        return JsonSerializer.Deserialize<AtlasData>(File.ReadAllBytes(fileName));
    }
}