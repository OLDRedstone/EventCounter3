# EventCounter3

A simple event counter for the Rhythm Doctor game.

![screenshot](image.png)

You can edit the configuration in `config.yaml`.

```yaml
countingMethod: Detailed # "Detailed" or "Simple"
# Detailed: Processes level files more slowly than "Simple" and is even slower when
# processing `.rdzip` files, but provides more detailed information.
# Simple: Only processes the file and counts event types.

pixelSize: 2
fontFamily: Arial
theme: 0 # You can customize the theme by editing `Assets/assets.png`.
language: en-us # You can edit translations in `Assets/Lang.[language-id].yaml`.
```

This project is intended to test the library [RhythmBase](https://github.com/RDCN-Community-Developers/RhythmToolkit) when using `countingMethod: Detailed`.
If you encounter any issues or have improvements, opening an issue or submitting a pull request is welcome.