#Calibration

Tool that can be used to calibrate interface between Leap Motion Controller and projector. Result is camera matrix from which can be computed translation from Leap coordinates in world space into projector coordinates in pixels.
##Build
- compiled into exe using py2exe

##Calibration.py
###Requirements
- python 2, i am using 2.7.6
- PyQT 4
- Leap API from Leap SDK, Leap.py must be put to both x86 and x64 folder. already included
    
    newest SDK version is available at https://developer.leapmotion.com/downloads
    used version - 2.2.2+24469
- numpy http://www.numpy.org/
- openCV (cv2) http://docs.opencv.org/trunk/doc/py_tutorials/py_setup/py_setup_in_windows/py_setup_in_windows.html, already included
    
##Usage
-Recommended setup 
![hw setup](https://github.com/jojkos/LeapMotion-interface/blob/master/setup.png)
- In appliacation you can choose width and height of projector image in pixels.
- Cycles is how many times you want to run the calibration pattern. Each has 3 * 3 points.
- Shift changes how far from edges the points are drawn.
- Head mount optimization toggles the same thing for Leap Motion . It is used because Leap Motion is upside down. https://developer.leapmotion.com/documentation/csharp/api/Leap.Controller.html#id7
- Calibrate lets you choose file you want the calibration to save in (inputs.txt if you want to use it later in the game)
- Load loads already saved calibration you choose.
- Reprojection lets you calculate reprojection error for the chosen calibration.
- To close calibration window, right click it and chose close.
    
example video of similar calibration
https://youtu.be/l7NUiP3t3F8    
 
#Game
Game created in Unity 3D based on https://unity3d.com/learn/tutorials/projects/space-shooter.
Controlling is modified to be used with Leap Motion and projector setup as shown on picture.
##Build
- compiled in Unity 3D

##Source - Unity 3D

###Requirements
- LeapMotionSpaceGame root folder contains several OpenCV dlls precompiled from opencv-2.4.10 build/x86/vc12/bin. In case of problems delete them and do your own build. You can then put them back into root folder or add path to them into system PATH variable.
- create inputs.txt with Calibration.py
    - put it in the Assets folder for using it in unity
    - put it in the game_Data to use the precompiled game 
- root folder
    - Leap.dll from Leap SDK
    - LeapCSharp.dll from Leap SDK      
- Assets folder
    - LeapCSharp.NET3.5.dll from Leap SDK
    - SimpleJSON.cs from https://code.google.com/p/json-simple/ used to read the inputs.txt file
    - OpenCvSharp.dll from https://github.com/shimat/opencvsharp/releases to be able to use OpenCV methods inside Unity's C#

example video
https://youtu.be/G3N2fOW8vmU    


assets from https://unity3d.com/learn/tutorials/projects/space-shooter

