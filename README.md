# Jpeg Compressor CS
This a new project for a native, Windows-based application written in C# that compresses JPEG images and offers flexibility with adjusting the quality level as desired and reducing overall file size. This application is an expansion/recreation of another C++ utility developed ([https://github.com/waltonsurratt/JpegCompressor]JpegCompressor) - this utility offers nearly the same benefits in regards to JPEG compression and optimizes memory usage, but comes with a more modern-looking UI for Windows 10/11 systems. This application is also built on .NET 10, bringing it up-to-date with all current Microsoft runtime standards.

# Version: 1.3.0
The tool currently includes the following features (initial release):
* Batch file processing
* Error handling for non-JPEG files
* File overwrite behavior
* Image Quality slider bar (linked to compression rate)
* Modern UI Design
* *NEW: Cancel Button (mid-compression cancellation support)

<img width="397" height="277" alt="image" src="https://github.com/user-attachments/assets/1ab39564-565c-49a3-82aa-6a9f6ba3853c" />

## Future Changes (Planned)
* ~~Cancellation support / Cancel Button (after compression process begins)~~
* ~~Batch file processing~~
* ~~Drag-and-Drop support~~
* ~~Integration of more advanced compression libraries (libjpeg-turbo)~~
* Check-for-updates feature
