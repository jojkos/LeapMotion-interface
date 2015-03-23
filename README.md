# LeapMotion-interface

Calibration.py
-------------------------------------
requirements
- python 2, using 2.7.6
- PyQT 4
- Leap API from Leap SDK in this structure for both x64 and x86
    ./LeapSDK/lib/x64/Leap.py
    ./LeapSDK/lib/x86/Leap.py

    newest SDK version is available at https://developer.leapmotion.com/downloads
    used version - 2.2.2+24469
- numpy http://www.numpy.org/
- openCV (cv2) http://docs.opencv.org/trunk/doc/py_tutorials/py_setup/py_setup_in_windows/py_setup_in_windows.html

usage
    - inputs.txt

 
Game
------------------------------------

requirements
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
    


assets from https://unity3d.com/learn/tutorials/projects/space-shooter
