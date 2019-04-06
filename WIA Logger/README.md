This folder contains helpers for logging Windows Image Acquisition (WIA) messages sent by Sony Alpha cameras (only A7III has been tested).
Sony sends messages using the `IWiaItemExtras::Escape` function which can be logged via an API monitor. There is a logger in this project but in its current state requires additional modification to be useful.

`SonyAlphaUSBLoader.cpp` can be compiled via `cl.exe /LD SonyAlphaUSBLoader.cpp` which can be found at `C:\Program Files (x86)\Microsoft Visual Studio XX.X\VC\bin\amd64\cl.exe` (where XX.X is the Visual Studio version)
Copy SonyAlphaUSBLoader.dll and SonyAlphaUSB.exe to the Sony Imaging Edge folder (along side all of the other exe/dll files) and then run `SonyAlphaUSB.exe wlog` (it may need to be ran as admin).

The "API Monitor" folder can be merged into the "API" folder of http://www.rohitab.com/apimonitor which can then be used to minitor all WIA functions.
