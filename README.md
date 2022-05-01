# SpoiledCat.UI

Base Editor window class and interfaces to make it easier to implement editor windows. The `BaseWindow` class provides virtual methods that split up layout and repaint phases into different calls:

- `OnDataUpdate` - Called during the layout phase. Use it to refresh and cache values to be used.
- `OnFirstRepaint` - Called on the very first repaint event after `OnEnable`
- `OnUI` - Called after `OnDataUpdate`, equivalent to `OnGUI` (always called whenever OnGUI would be called)
