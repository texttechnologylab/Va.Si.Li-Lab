[![Paper_HCII](http://img.shields.io/badge/paper-HCII--2023-B31B1B.svg)](https://doi.org/10.1007/978-3-031-35741-1_39)
[![Paper_HT](http://img.shields.io/badge/paper-HT--2023-F31B1B.svg)](https://doi.org/10.1145/3603163.3609076)
[![Conference](http://img.shields.io/badge/conference-HCII--2023-4b44ce.svg)](https://2023.hci.international/)
[![version](https://img.shields.io/github/license/texttechnologylab/Va.Si.Li-Lab)]()
![GitHub release (with filter)](https://img.shields.io/github/v/release/texttechnologylab/Va.Si.Li-Lab)

# Va.Si.Li-Lab
a **V**R-L**a**b for **Si**mulation-based **L**earn**i**ng

# Abstract
Va.Si.Li-Lab was established as part of the project "Digital Teaching and Learning Lab" (DigiTeLL) at the Goethe University Frankfurt. 



# Va.Si.Li-Lab - Team
* Prof. Dr. Alexander Mehler (Leader)
* Giuseppe Abrami
* Mevlüt Bagci
* Dr. Alexander Henlein
* Patrick Schrottenbacher
* Christian Spiekermann

# Installation

## Open in unity
* https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022
* https://developer.oculus.com/downloads/package/meta-avatars-sdk/

## Unity
Add the git package in the Unity Package Manager
```
https://github.com/texttechnologylab/Va.Si.Li-Lab.git#upm
```
### Quick-start
1. Import the samples from the Unity package and open the `Start` scene.
2. Open the `Social Network Scene` prefab and configure the `Connection Defintion` in the RoomClient to point to your own Ubiq-Server installation, or for quick testing you can switch it out to the `Nexus Connection Definition` which is the server provided by the UCL (some features will be missing)
3. Configure the api url to point to your server in the `Scene Manager` script located in the `SceneManager` GameObject.
4. Click play!

### Advanced
1. Create a new scene to act as your starting scene.
2. Import the `Player`, `Scene Selecter` and `Social Network Scene` prefab into your scene.
3. Create another scene and add it to your build settings as well as the database.
4. Click play!


# Cite
If you want to use the project or the corpus, please quote this as follows:

Alexander Mehler, Mevlüt Bagci, Alexander Henlein, Giuseppe Abrami, Christian Spiekermann, Patrick Schrottenbacher, Maxim Konca, Andy Lücking, Juliane Engel, Marc Quintino, Jakob Schreiber, Kevin Saukel and Olga Zlatkin-Troitschanskaia. (2023). "A Multimodal Data Model for Simulation-Based Learning with Va.Si.Li-Lab." Digital Human Modeling and Applications in Health, Safety, Ergonomics and Risk Management, 539–565. [[LINK](https://doi.org/10.1007/978-3-031-35741-1_39)]

Giuseppe Abrami, Alexander Mehler, Mevlüt Bagci, Patrick Schrottenbacher, Alexander Henlein, Christian Spiekermann, Juliane Engel and Jakob Schreiber. (2023). "Va.Si.Li-Lab as a Collaborative Multi-User Annotation Tool in Virtual Reality and Its Potential Fields of Application." Proceedings of the 34th ACM Conference on Hypertext and Social Media. [[LINK](https://doi.org/10.1145/3603163.3609076)] [[PDF](https://dl.acm.org/doi/pdf/10.1145/3603163.3609076)]

# BibTeX


