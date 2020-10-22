# SimpleIO 

Fork of [NiceIO](https://github.com/lucasmeijer/NiceIO) with major changes to the original, hence the name change. Changes include:

- Path representation is now a struct - `class NPath` => `struct SPath`
- All filesystem operations go through a `IFileSystem` abstraction, with a default implementation using the `System.IO` API. The filesystem implementation can be switched out at compile time or runtime.
- File and directory operations that required absolute paths (due to underlying System.IO API restrictions) now support relative paths, which are internally resolved to absolute paths using the value returned by `CurrentDirectory`.
- The Editor and Runtime classes are the same, except that the Editor implementation uses `Mono.Posix` in order to resolve filesystem paths.
- File read/write methods added - `Read/WriteAllBytes`, `OpenRead/Write`
- System information properties added: `HomeDirectory`, `LocalAppData`, `CommonAppData`, `SystemTemp`
- More OS detection added - `IsWindows`, `IsLinux`, `IsMac`


Basic usage:
```c#
//paths are immutable
SPath path1 = new SPath(@"/var/folders/something");
// /var/folders/something

//use back,forward,or trailing slashes,  doesnt matter
SPath path2 = new SPath(@"/var\folders/something///");
// /var/folders/something

//semantically the same
path1 == path2;
// true

// ..'s that are not at the beginning of the path get collapsed
new SPath("/mydir/../myfile.exe");
// /myfile.exe

//build paths
path1.Combine("dir1/dir2");
// /var/folders/something/dir1/dir2

//handy accessors
SPath.HomeDirectory;
// /Users/lucas

//all operations return their destination, so they fluently daisychain
SPath myfile = SPath.HomeDirectory.CreateDirectory("mysubdir").CreateFile("myfile.txt");
// /Users/lucas/mysubdir/myfile.txt

//common operations you know and expect
myfile.Exists();
// true

//you will never again have to look up if .Extension includes the dot or not
myfile.ExtensionWithDot;
// ".txt"

//getting parent directory
SPath dir = myfile.Parent;
// /User/lucas/mysubdir

//copying files,
myfile.Copy("myfile2");
// /Users/lucas/mysubdir/myfile2

//into not-yet-existing directories
myfile.Copy("hello/myfile3");
// /Users/lucas/mysubdir/hello/myfile3

//listing files
dir.Files(recurse:true);
// { /Users/lucas/mysubdir/myfile.txt, 
//   /Users/lucas/mysubdir/myfile2, 
//   /Users/lucas/mysubdir/hello/myfile3 }

//or directories
dir.Directories();
// { /Users/lucas/mysubdir/hello }

//or both
dir.Contents(recurse:true);
// { /Users/lucas/mysubdir/myfile.txt, 
//   /Users/lucas/mysubdir/myfile2, 
//   /Users/lucas/mysubdir/hello/myfile3, 
//   /Users/lucas/mysubdir/hello }

//copy entire directory, and listing everything in the copy
myfile.Parent.Copy("anotherdir").Files(recurse:true);
// { /Users/lucas/anotherdir/myfile, 
//   /Users/lucas/anotherdir/myfile.txt, 
//   /Users/lucas/anotherdir/myfile2, 
//   /Users/lucas/anotherdir/hello/myfile3 }

//easy accesors for common operations:
string text = myfile.ReadAllText();
string[] lines = myfile.ReadAllLines();
myFile.WriteAllText("hello");
myFile.WriteAllLines(new[] { "one", "two"});
```

SimpleIO is MIT Licensed.
