# SonyAlphaUSB

This (WIP) project aims to control Sony Alpha cameras via the USB connection on Windows. Sony A7 III is the only camera which has been tested so far.

TODO:
- Implement all of the main/sub settings (f-number, focus, etc)
- Find out how to gather all of the current setting values
- Determine the format of the live view data to allow displaying of the live view image data
- Find hidden settings (zoom isn't available on the Sony Imaging Edge software? is it available in the protocol? need a power zoom lense to test)

## How it works

The Windows Image Acquisition (WIA) API is used to control the camera via USB in the exact same way that the Sony Imaging Edge software does.

## Disclaimer

Sony doesn't provide an official API for interacting with the USB protocol. Using this code is done at your own risk. Requests should be sent to the camera in a timely manner to avoid any issues.
