# DAW-and-Tuner
This repository contains a real-time guitar tuner application and the foundational elements of a basic Digital Audio Workstation (DAW), both developed in C# using the .NET framework.

Guitar Tuner:

The tuner utilizes audio input from a microphone to detect the fundamental frequency of a guitar string. It employs signal processing techniques (initially FFT, with potential for Autocorrelation or more advanced methods) to identify the pitch and displays the detected note, along with a visual representation of its intonation (cents deviation from the target frequency).

Key Features (Tuner):

Real-time Pitch Detection: Analyzes audio input to determine the current pitch.
Note Display: Shows the detected musical note (e.g., E2, A4).
Cent Deviation Meter: Visually indicates how sharp or flat the detected pitch is relative to the target frequency in cents.
Target Frequency Mapping: Includes a predefined set of target frequencies for standard guitar tunings.
Basic UI: Provides a visual interface for the tuner, including a needle meter and note labels.
(In Progress) Stability Enhancements: Ongoing efforts to improve the accuracy and stability of the pitch detection algorithm.

Basic DAW:

The DAW section of this project is in its early stages and currently provides rudimentary audio recording and playback capabilities. It serves as a starting point for a more comprehensive audio workstation.

Key Features (DAW - Initial Stage):

Audio Recording: Ability to capture audio input from a microphone.
Basic Playback: Functionality to play back recorded audio.
Waveform Visualization: Displays the recorded audio as a waveform.
(In Progress) Multi-track Support (Conceptual): The architecture is being designed with potential future multi-track capabilities in mind.
Technology Stack:

C# .NET Framework: The primary programming language and development platform.
NAudio: A .NET audio library used for handling audio input and output.
