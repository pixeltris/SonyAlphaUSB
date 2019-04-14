# SonyAlphaUSB

This (WIP) project aims to control Sony Alpha cameras via the USB connection on Windows. Sony A7 III is the only camera which has been tested so far.

This can be used to control the following:

- Simulating shutter button half/full press to capture photos (which get transfered to the PC)
- Starting / stopping a video recording
- Modify f-number, ISO, shutter speed, AEL, FEL, focus area, focus area spot, EV, flash, output image file format (jpeg/raw), output image size, picture effect, DRO/HDR, aspect ratio, focus mode, focus distance, focus magnifier, shooting mode, white balance (color temp, AB, GM), drive mode, flash mode, metering mode. TODO: zoom (have yet to get a power zoom lense to test)

## How it works

The Windows Image Acquisition (WIA) API is used to control the camera via USB in the exact same way that the Sony Imaging Edge software does. Under the hood this should be PTP which is what gphoto2 uses. See Sony's opcodes [here](https://github.com/gphoto/libgphoto2/blob/2e5e43430afc62aed8e7950dd4ab48080200d786/camlibs/ptp2/ptp.h#L2352) or [here](https://github.com/pixeltris/SonyAlphaUSB/blob/master/SonyAlphaUSB/Ids.cs)

## Disclaimer

Sony doesn't provide an official API for interacting with the USB protocol. Using this code is done at your own risk. Requests should be sent to the camera in a timely manner to avoid any issues.
