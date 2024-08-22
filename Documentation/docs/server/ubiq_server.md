# Ubiq Server
The networking between multiple user is handled by [Ubiq](https://ubiq.online/).
([Documentation](https://ucl-vr.github.io/ubiq/))
([GitHub](https://github.com/UCL-VR/ubiq/))

``` bibtex
@inproceedings{friston2021ubiq,
  title={Ubiq: A system to build flexible social virtual reality experiences},
  author={Friston, Sebastian J and Congdon, Ben J and Swapp, David and Izzouzi, Lisa and Brandst{\"a}tter, Klara and Archer, Daniel and Olkkonen, Otto and Thiel, Felix Johannes and Steed, Anthony},
  booktitle={Proceedings of the 27th ACM symposium on virtual reality software and technology},
  pages={1--11},
  year={2021}
}
```

## Installation
### Docker
We use a slightly modified version of the official Ubiq.
One reason for this is that the original was not equipped with a corresponding Docker file.
The other reason is that we had to add some functions to the original Ubiq.
Now there is also an official docker build variant, which is why we want to integrate ubiq directly in a new version.

```bash
git clone https://github.com/texttechnologylab/ubiq.git
cd Node
docker build -t ubiq .
docker run -p 8009:8009 ubiq
# (docker run -d --restart unless-stopped -p 8009:8009 ubiq)
```


## Unity

`SocialNetworkScene > RoomClient`