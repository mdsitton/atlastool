# Clone Hero Texture Atlas Tool
## A tool for assisting in modifying game textures in Clone Hero

## Usage:
This program requires you to invoke it and pass commands to it via a command line.

#### Commands:
`-h` / `--help`

Displays information about available commands.

`-x -i <path1> -o <path2>` / `--export --input <path1> --output <path2>`

Gets the sprite atlas from the input path and exports it and individual sprites to the output path.

Input directory can be either the data.unity3d file, resources.assets file, or `Clone Hero_Data` folder.

A new folder named after the game version will be created within the output path. If no path is specified, it defaults to atlastool's folder.

`-c -i <path>` / `--combine --input <path>`

Combines the sprites from the `sprites` folder in the input folder into a new atlas image, which gets placed into the input folder.

#### Examples:
Extracting:

`-x -i C:\Games\Clone Hero\Clone Hero_Data\unity.data3d -o .\extracted`

`-x -i %APPDATA%\Clone Hero Launcher\gameFiles\Clone Hero_Data`

Combining:

`-c -i .\v.23.2.2`

#### Note:
For now, the new atlas will need to be added in using [Unity Asset Bundle Extractor Avalonia](https://github.com/nesrak1/UABEA).
Use the [latest nightly build](https://nightly.link/nesrak1/UABEA/workflows/dotnet-desktop/master/uabea-windows.zip) instead of the latest release from the Releases page, as the current latest release ("second release") doesn't currently support the version of Unity that CH uses.
(Be warned that since these are nightly builds, there may be bugs.)

## License
This project is licensed under the MIT license. See [license.txt](https://github.com/mdsitton/atlastool/blob/master/license.txt) for details.

See [thirdparty.txt](https://github.com/mdsitton/atlastool/blob/master/thirdparty.txt) for licenses of third-party assets.

## Acknowledgements
Perfare's [AssetStudio](https://github.com/Perfare/AssetStudio) for texture extraction
