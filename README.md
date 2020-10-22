## Quick Console

Need to run some code in Unity but can't because changing any editor or game code will cause everything to be recompiled? Want to check the state of an object but the only way to access it is via code, and you're tired of adding menu entries to run random code?

Quick console is a Unity window similar to Visual Studio's Immediate window, where you can type code to be compiled and executed without having to recompile anything else on your project. The code is compiled into a separate assembly and loaded into the current C# domain, so it doesn't cause anything else to be recompiled and doesn't affect the state of anything in the editor. You can use it to evaluate scene objects, run actions, do whatever you would otherwise do in a Unity editor or runtime script.
