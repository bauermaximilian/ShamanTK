# ShamanTK

A simple toolkit for creating games and other multimedia applications in C#.

## About

Importing a 3D model, drawing that on screen, playing back an MP3 file - many tasks that are required in writing games are anything but trivial, often even seem like some sort of black magic. Luckily, there's a lot of good frameworks and game engines that do these things for you, give you tools helping you to make your vision reality. But what is the right tool for the job - when the easy-to-use engine predetermines your project structure too strictly, and the framework has a steep learning curve to understand its million features, seemingly just adding another layer of complexity? When all you need is some help to conjure some sight and sound on your machine - when you want something that is easy to use, understand and extend... the Shaman Toolkit might just be what you're looking for.

## Highlights

- Easy-to-use 2D/3D rendering pipeline with various shading modes, render texture buffers and much more
- Background resource import with integrated support for standard formats (like GLTF, MP3, PNG, JPG...)
- Integrated keyframe animation system, supporting full character animation
- Sprite text rendering with built-in sprite font generator for TrueType/OpenType fonts
- Support for controllers, mapped to a standard Xbox layout
- Audio playback for clips and streams
- Platform-independent and completely written in .NET Core 3.1
- Comes with a complete API documentation and mildly entertaining example applications

## How to use

Until a nuget package is released, you should clone this repository onto your machine. Sometimes, it might be required to open the project solution and build it at least once. Afterwards, you can create your own projects, from which you just have to add a project reference to the ShamanTK project. Then just override the ``ShamanApp`` base class - and you're good to go!

## License

ShamanTK is licensed under the LGPLv3. This should ensure that the toolkit and every improvement of it will be available for everyone, but won't make it overly complicated for anyone who's just using and developing something with it.

## Current State

After its initial release almost year ago (under the old name "Eterra Framework"), the project has been improved, extended and tested - and it's still under heavy development and the foundation for many of my other and upcoming projects - so stay tuned!

### Development roadmap

- Update the demo applications - right now, these are still using the old Eterra API
- Convert the project to use nullable reference types
- Proofread the documentation and fix the related warnings
- Extend the sound system to support locational sound and other effects
- Create unit tests