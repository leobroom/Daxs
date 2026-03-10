# Daxs

**Daxs** is a Rhino plugin/package/app that brings gamepad support to Rhino.

Daxs makes navigating and interacting with Rhino feel as fluid and natural as moving inside a 3D game engine. It is designed to give Rhino a stable gamepad interface and customizable mappings, **without intefering with Rhinos functionality**.

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

## WHY
Navigating precicely complex models can sometimes be frustrating, especially within enclosed interior geometries such as architectural models.
With gamepad support, navigation through geometry becomes significantly smoother and more natural (for us gamers). Most people already have spare controllers lying around. Simply plug in an old one and start using it.

## Key Features
- Free-flight navigation through the Rhino scene.
- Walk mode - Movement constrained to a ground plane or a navigation mesh.
- Plug and Play! Connect a gamepad over bluetooth - thats it! It will be automatically recognised.
- No movement, no loose on performance. Daxs runs a high-frequency input runtime that processes controller input independently of Rhino's UI thread.
- Fully Customizable Button Mapping, including Rhino macros
 
## Installation
Coming soon: 
- packaged .yak installer for Rhino.

## License
MIT License. See [MIT license](https://github.com/leobroom/Daxs/blob/main/LICENSE) for details.

## Third-Party Dependencies
### SDL3
[Github page](https://github.com/libsdl-org/SDL)  
Copyright © Sam Lantinga  
Licensed under the zlib License  
### SDL3-CS Wrapper
[Github page](https://github.com/edwardgushchin/SDL3-CS)  
Copyright © Eduard Gushchin  
Licensed under the zlib License  
### RhinoCommon SDK
https://developer.rhino3d.com/  
© Robert McNeel & Associates
