# Daxs

**Daxs** is a Rhino plugin/package/app that brings gamepad support to Rhino.

It is built on top of SDL 3 (via a SDL3-CS wrapper) and supports hundreds of gamepad models and variants out of the box — automatically using SDL’s unified input layer.

Daxs is designed to give Rhino a stable gamepad interface with ergonomic APIs and customizable mappings

## Key Features

### Massive Gamepad Support

Daxs supports theoretically **hundreds of** gamepads thanks to SDL 3’s controller database:

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

## Installation
Coming soon: 
- packaged .yak installer for Rhino.

## License

MIT License. See "MIT license" for details.

## Third-Party Dependencies

### SDL3
[Github page](https://github.com/libsdl-org/SDL)  
Copyright © [Sam Lantinga](slouken@libsdl.org)  
Licensed under the zlib License  
### SDL3-CS Wrapper
[Github page](https://github.com/edwardgushchin/SDL3-CS)  
Copyright © [Eduard Gushchin](eduardgushchin@yandex.ru)  
Licensed under the zlib License  
### RhinoCommon SDK
[developer.rhino3d.com](https://developer.rhino3d.com/)
© Robert McNeel & Associates
