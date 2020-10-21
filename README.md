# Collection of Unity utilities

## Environment

[](tree/master/src/com.spoiledcat.environment)

## Logging

[](tree/master/src/com.spoiledcat.logging)

## Quick Console

[](tree/master/src/com.spoiledcat.quick-console/README.md)

Need to run some code in Unity but can't because changing any editor or game code will cause everything to be recompiled? Want to check the state of an object but the only way to access it is via code, and you're tired of adding menu entries to run random code?

Quick console is a Unity window similar to Visual Studio's Immediate window, where you can type code to be compiled and executed without having to recompile anything else on your project. The code is compiled into a separate assembly and loaded into the current C# domain, so it doesn't cause anything else to be recompiled and doesn't affect the state of anything in the editor. You can use it to evaluate scene objects, run actions, do whatever you would otherwise do in a Unity editor or runtime script.

## SharpZipLib

[](tree/master/src/com.spoiledcat.sharpziplib)

ICSharpCode.SharpZipLib source package, compatible with Unity 5.6 and above, with a different top level C# namespace (to avoid name conflicts), and including the Tar and Zip implementations that are missing from the binary versions included in some recent Unity versions.

## Simple IO

[](tree/master/src/com.spoiledcat.simpleio/README.md)

Fork of [NiceIO](https://github.com/lucasmeijer/niceio), a nice uncomplicated Unity-friend IO API.

## Simple Json

[](tree/master/src/com.spoiledcat.simplejson/README.md)

Fork of SimpleJson.

This fork adds support for:
- Serialization of structs
- Serialization of private fields and properties
- Serialization of DateTime and DateTimeOffset with multiple ISO formats
- Custom casing and naming implementations. It provides extension methods ToJson and FromJson with casing and visibility choices, and users can implement their own strategies for custom serialization.

## Threading

[](tree/master/src/com.spoiledcat.threading/README.md)

SpoiledCat.Threading is a TPL-based threading library that simplifies running asynchronous code with explicit thread and scheduler settings.

## UI

[](tree/master/src/com.spoiledcat.ui/README.md)

Base Editor window class and interfaces to make it easier to implement editor windows. The `BaseWindow` class provides virtual methods that split up layout and repaint phases into different calls:

- `OnDataUpdate` - Called during the layout phase. Use it to refresh and cache values to be used.
- `OnFirstRepaint` - Called on the very first repaint event after `OnEnable`
- `OnUI` - Called after `OnDataUpdate`, equivalent to `OnGUI` (always called whenever OnGUI would be called)

## Utilities

[](tree/master/src/com.spoiledcat.utilities/README.md)

Miscellaneous utilities, string and stream extensions, argument validation helpers, and a `TheVersion` class that can parse and compare version strings. `TheVersion.Parse("2.1.32.5")`
