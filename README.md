# ![Daxs Logo](Resources/icon.png) Daxs
> ⚠️ **Early Development Version:** Daxs is currently in a very early stage and features, behavior, and APIs may change significantly.

**Daxs** is a **Rhino 8** plugin/package/app that brings gamepad support to Rhino.

Daxs (pronounced [[daˈks]](https://youtu.be/_Uw-8_C8lpg?si=rvCSB4eXKVYq_Fz7&t=7)  - from the German "Dachs"- badger) makes navigating and interacting with Rhino feel as fluid and natural as moving inside a 3D game engine. It is designed to give Rhino a stable gamepad interface and customizable mappings, **without intefering with Rhinos functionality**.

| <img src="https://raw.githubusercontent.com/leobroom/Daxs/main/Resources/fly.png" alt="Daxs Fly Mode" width="24"> Daxs Fly Mode  | <img src="https://raw.githubusercontent.com/leobroom/Daxs/main/Resources/Walk.png" alt="Daxs Walk Mode" width="24"> Daxs Walk Mode |
| ------------- | ------------- |
| <img src="https://raw.githubusercontent.com/leobroom/Daxs/main/Resources/Wiki/Daxs_FlyMode2.gif" alt="Daxs Fly Mode">  | <img src="https://raw.githubusercontent.com/leobroom/Daxs/main/Resources/Wiki/Daxs_WalkMode2.gif" alt="Daxs Flywalk Mode"> |
| Fly through your model  | Walk through your model via navigation mesh, or a plane (first-person view)  |

## Gamepads supported
Daxs is built on top of SDL 3 (via a SDL3-CS wrapper) and supports hundreds of gamepad models and variants out of the box — automatically using SDL’s unified input layer.
Supported Gamepads:

- Xbox 360 / Xbox One / Xbox Series
- PlayStation 4 / PlayStation 5
- Nintendo Switch Pro
- Steam Controller
- Logitech / Razer / PowerA / 8BitDo / Hori / PDP
- Rock Candy, GameSir, Nacon, Scuf, MSI, etc.
- Generic HID gamepads
- Bluetooth & wired models
- Vendor-specific variants and clones

SDL automatically normalizes controller layouts → Daxs does the rest to communicate with Rhino.

## Why
Navigating precisely complex models can sometimes be frustrating, especially within enclosed interior geometries such as architectural models.
With gamepad support, navigation through geometry becomes significantly smoother and more natural (for us gamers). Most people already have spare controllers lying around. Simply plug in an old one and start using it.

## Key Features
- Free-flight navigation through the Rhino scene.
- Walk mode - Movement constrained to a ground plane or a navigation mesh.
- Plug and Play! Connect a gamepad over bluetooth - thats it! It will be automatically recognised.
- No movement, no loose on performance. Daxs runs a high-frequency input runtime that processes controller input independently of Rhino's UI thread.
- Fully Customizable Button Mapping, including Rhino macros
 
## Installation & Version
- **Right now just Rhino 8 on Windows is supported.**
- Update your Rhino first to the newest version!
- packaged .yak installer for Rhino for now. [Check the latest release](https://github.com/leobroom/Daxs/releases)
- Package Manager: You can install Dax over the Rhino Package Manager
- Food For Rhino: Comming soon...

## How to get started
-> [Wikipage - How to get started](https://github.com/leobroom/Daxs/wiki#how-to-get-started)

## Documentation
-> [Wikipage - Documentation](https://github.com/leobroom/Daxs/wiki#documentation)

## Support
- Create in Github directly an Issue - or:
- [Visit the official support thread inside the official McNeel/Rhino forum](https://discourse.mcneel.com/t/daxs-plugin-gamepad-support-for-rhino/217538?u=leonbrohmann)

## License
MIT License. See [MIT license](https://github.com/leobroom/Daxs/blob/main/LICENSE) for details.

## Third-Party Dependencies
### SDL3
[Github page](https://github.com/libsdl-org/SDL)  
Copyright © Sam Lantinga  
Licensed under the zlib License  
### SDL_GameControllerDB
[Github page](https://github.com/mdqinc/SDL_GameControllerDB)  
Copyright © Sam Lantinga  
Licensed under the zlib License  
### SDL3-CS Wrapper
[Github page](https://github.com/edwardgushchin/SDL3-CS)  
Copyright © Eduard Gushchin  
Licensed under the zlib License  
### RhinoCommon SDK
https://developer.rhino3d.com/  
© Robert McNeel & Associates
