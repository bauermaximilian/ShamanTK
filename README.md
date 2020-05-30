# Eterra Framework

An easy-to-use framework for creating games and multimedia applications in C#.

## About

Programming games isn't simple. Not by a long shot. When I first jumped into the topic as a novice programmer, I was surprised to discover the incredible depth of skills and knowledge required by a game developer. It not only requires efficient programming, but also proficiency with complicated mathematics, graphical formulae, and much more. All skills I didn't possess at the time. Even the well-designed frameworks seemed to add another layer of complexity, yet using a game engine straight out of the box didn't feel right. It would be as though the game wasn't programmed by me. So, I began to experiment. After two years of development, and with countless iterations, late nights, setbacks (and lots of profanity), it is finally ready. Everything I would have wanted from a game framework. I present: Eterra.

## Highlights

- Asynchronous resource import from various source file systems and formats
- Easy-to-use methods for common graphics, audio, control and IO operations, including:
  - Keyframe animations (with support for bone deformations)
  - Text rendering
  - Audio playback (and streaming for longer sound files)
  - Rendering to textures
  - Other cool things I forgot
- Base framework is in .NET Standard 2.0 and doesn't use any dependencies (besides System.Numerics.Vectors)
- Usable by ordinary mortals without a major in graphics programming or linear algebra
- Surprisingly detailed documentation and examples

## Usage and structure

For every development with _Eterra_, you will need to add a reference to the base _Eterra Framework_ - this project. For easy access to the functionality of _Eterra_, your application should then build on the `EterraApplicationBase` class in the main `Eterra` namespace.

Every application derived from the `EterraApplicationBase` class is initialized and started with an `IPlatformProvider` instance - these are provided by the available _platform modules_ for the _Eterra Framework_ in the `Eterra.Platforms` namespace, outsourced into the seperate project solution you can find [here](https://github.com/bauermaximilian/Eterra.Platforms). Depending on what operating system and device your application should run on, you need to reference the right project from that solution.

A detailed demonstration on how this works and what functionality the _Eterra Framework_ provides can be found in this mildly entertaining _Eterra Demo_ project you can find [here](https://github.com/bauermaximilian/Eterra.Demo).

## License

The _Eterra Framework_ is licensed under the LGPLv3. This should ensure that the framework and every improvement on it will be available for everyone, but won't make it overly complicated for anyone who's just using _Eterra_ and developing something with it.

## Current state

Currently, the project is still in early alpha. Some parts of the framework or the platform providers aren't tested properly, other parts are still subject to change or need heavy refactoring. It is, however, actively developed and will serve as foundation for my future game projects - so stay tuned!

### Development roadmap

- Fix the 600 XmlDoc-related warnings
- Create platform providers for Linux and MacOS
- Extend the sound system (and fix some remaining bugs in it)
- Extend, refactor and document the Scene/Entity classes
- Create a test suite

## Special thanks to

Jacob Bryden - for proofreading, testing but most of all motivating me to do this thing.