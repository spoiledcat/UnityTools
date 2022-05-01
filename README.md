# SpoiledCat Utilities

Miscellaneous utilities, string and stream extensions, argument validation helpers, and a `TheVersion` class that can parse and compare version strings, be they SemVer or more complex variations.

Examples:

```c#
// A version can have up to 4 parts, where only the first part (Major) is required to be a number
version = TheVersion.Parse("2.1.32.5");
Assert.AreEqual(2, version.Major);
Assert.AreEqual(1, version.Minor);
Assert.AreEqual(32, version.Patch);
Assert.AreEqual(5, version.Build);
Assert.AreEqual(null, version.Special);

// as soon as a part has non-numeric characters, parsing stops
version = TheVersion.Parse("2.1alpha1");
Assert.AreEqual(2, version.Major);
Assert.AreEqual("1alpha1", version.Special);
Assert.AreEqual(0, version.Minor);
Assert.AreEqual(0, version.Patch);
Assert.AreEqual(0, version.Build);

// version comparsin takes alphanumerics into account
TheVersion.Parse("0.33.3-alpha") < TheVersion.Parse("0.33.3-beta")
// true

// IsUnstable can be used as a SemVer preview check
TheVersion.Parse("1.2alpha1").IsUnstable
// true

TheVersion.Parse("1.2").IsUnstable
// false

// partial version strings get the rest of the parts filled in with 0, so partial comparisons work
TheVersion.Parse("2") == TheVersion.Parse("2.0.0.0")
// true

// if a version can't be parsed, it will be equal to TheVersion.Default
TheVersion.Parse("bla") == TheVersion.Default
// true

```
