# Daxs

**Daxs** is a Rhino plugin/package that brings full, modern gamepad support to Rhino.

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

Third-Party Dependencies

Daxs uses:

- SDL 3 – zlib License
- SDL3-CS Wrapper – zlib License
- RhinoCommon SDK – © Robert McNeel & Associates
