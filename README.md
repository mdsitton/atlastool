# Clone Hero Texture Atlas Tool
## A tool for modifying game textures in Clone Hero

## Usage:
This program requires you to invoke it and pass commands to it via a command line.

#### Commands:

`-x -i <data.unity3d/Clone Hero_Data path>`

Extract the game atlas and textures from a game install, or from any data.unity3d file.
It will generate a folder with the version number the atlas/textures are from, which it will extract the atlas and textures into.
You can then go into the `<version number>\sprites` folder to modify textures as desired.

`-c -i <folder path>`

Recombine the textures from the specified folder into a new atlas.

#### Note:
For now, the new atlas will need to be added in using [Unity Asset Bundle Extractor Avalonia](https://github.com/nesrak1/UABEA).
(Use the [latest build](https://nightly.link/nesrak1/UABEA/workflows/dotnet-desktop/master/uabea-windows.zip), the current latest release ("second release") doesn't currently support the version of Unity that CH uses.)