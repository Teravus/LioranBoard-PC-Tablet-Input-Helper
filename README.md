# LioranBoard-PC-Tablet-Input-Helper
Some PC tablets in Microsoft Windows 10 send 'tap gesture' events too fast for LioranBoard to process.  This project aims to send the input at a speed that registers properly.

It works by hooking into the global mouse events in windows and monitors for mouse activity.   When it finds that you have tapped something, it sends mouse click events using the Windows API slow enough for LioranBoard to process.

Requires admin privileges to run because it hooks into the windows events and clicks your mouse.

Built using the .NET framework version 4.7.2.   You can download that [here](dotnet.microsoft.com/download/dotnet-framework/net472)

**Note:** If you are using a mouse to click your LioranBoard Stream Deck (PC), you will not want to run this because it will click your buttons twice when clicking a mouse.  *This is strictly for using a touch screen only.*

## To Build The Solution;

 - Make sure you have Visual Studio 2019 installed [(Community edition is OK)](visualstudio.microsoft.com/vs/community/)
 - Open Visual Studio in Admin mode.  (Run as Administrator)
 - Open LioranBoardTabletInputStaller.sln.    
 - Rebuild Solution.
 - Look in your output window for the location where it saved the compiled executable.

## Using The Program
When you launch this tool, it will ask for permissions elevation.   You will then be presented with a screen that gives you the steps.   Follow the easy on-screen steps.  Once you have completed the steps and the status boxes are green, you can use your touch screen to tap LioranBoard Stream Deck (PC) buttons.   If you would like to switch to your mouse, make sure to click the 'Disable' button in step 3 so you don't click your LioranBoard buttons twice.

## Touch Accuracy Setting

Almost all tablets have an accuracy of at least 16 pixels around your touch point.   Some are more accurate.   When you load the program, you can set the 'Accuracy' in the user interface. This is a 'padding' square around where your finger tapped where this tool will recognize the tap gesture.  If you release the tap outside of this square, it will not register as a tap.    If you make this square too big, the tap gesture may happen multiple times.


## Mouse Click Delay Setting

Windows tap gestures happen too fast for LioranBoard to process the Left Mouse down event.   This is the delay between the mouse click sequence 'Move' -> 'Left Mouse Button Down' -> Left Mouse Button up.  

