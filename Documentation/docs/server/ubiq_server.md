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

The server lives in the
[Va.Si.Li-Lab-backend](https://github.com/texttechnologylab/Va.Si.Li-Lab-backend)
monorepo under `Ubiq-server/`.

```bash
git clone https://github.com/texttechnologylab/Va.Si.Li-Lab-backend.git
cd Va.Si.Li-Lab-backend/Ubiq-server
docker build -t ubiq .
docker run -d --restart unless-stopped -p 8009:8009 -p 8011:8011 ubiq
```

!!! info "Configuration and full reference"
    Ports, the layered `nconf` configuration and the `iceservers` block are
    documented at
    **[Ubiq Server](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/ubiq_server/)**
    in the backend documentation.

    Deployment overrides belong in `config/local.json`, which is not tracked -
    not in the committed `config/default.json`.


## Unity

The client reads the server address from *Ubiq Server* (a Ubiq `ConnectionDefinition`) in
`Assets/Resources/VasiliEndpoints.asset`. Create the file with *Tools > Va.Si.Li > Create Endpoints Config*. The
`VasiliServerConnection` component on the `RoomClient` of the `Social Network Scene` prefab applies it at start-up. If
the file is missing, the client falls back to `127.0.0.1:8009` and logs a warning.

## Docker Compose

The Ubiq server is also part of the combined
[InterView deployment stack](https://texttechnologylab.github.io/InterView/services/ubiq/),
which starts it alongside the other backend services with restart policies and
versioned configuration.
