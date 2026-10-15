# Systems using Va.Si.Li-Lab

A list of Systems built on Va.Si.Li-Lab, as modules of this repository or as separate applications. Papers and BibTeX are listed
under [Publications](publications.md#systems-using-vasili-lab).

| System | What it does | Paper | Part of |
|---|---|---|---|
| [InterView](#interview) | VR interviews: a participant in VR, the interviewer in a web browser | [Preprint](https://doi.org/10.2139/ssrn.7459949) | [FACES](projects/faces.md) |
| [ReEmote](#reemote) | Emotion representation for avatars and their environment | [XR Salento 2026](publications.md#reemote) (accepted) | [FACES](projects/faces.md) |
| [Va.Si.Li-ES](#vasili-es) | Event system: triggers, environment and weather changes, voting and feedback | [HT '24](https://doi.org/10.1145/3648188.3675154) | This repository (`Assets/Lab/EventSystem`) |
| [VR-ParlExplorer](#vr-parlexplorer) | Plenary debates in VR with chatbot members of parliament | [HT '25](https://doi.org/10.1145/3720553.3746672) | |
| [News in Time and Space](#news-in-time-and-space) | Geospatial hypertext: global news events on a virtual globe | [NRHM 2025](https://doi.org/10.1080/13614568.2024.2383601) | |
| [Viki LibraRy](#viki-library) | A collaborative VR library for browsing and reading hypertext | [NRHM 2025](https://doi.org/10.1080/13614568.2024.2383581) | |
| [Semantic Scene Builder](#semantic-scene-builder) | Text-to-3D scene generation, edited in VR | [HCII 2023](https://doi.org/10.1007/978-3-031-35748-0_32) | [ViCom](projects/vicom.md) |
| [FraGA](#fraga) | Frankfurt VR Gesture–Speech Alignment corpus, recorded in Va.Si.Li-Lab | [SemDial 2025](http://semdial.org/anthology/Z25-Luecking_semdial_3316.pdf) | [ViCom](projects/vicom.md) |

## InterView

A platform for VR survey interviews. The participant wears a headset and sits across from the interviewer's avatar;
the interviewer works from an ordinary browser. Speech, gaze, posture, hand movement, facial expression and the answers
are captured on a shared clock. InterView adds a WebRTC media layer (Janus), a browser-based interviewer console and a
token-driven session model to Va.Si.Li-Lab.

- Platform documentation: [texttechnologylab.github.io/InterView](https://texttechnologylab.github.io/InterView/)
- VR client setup: [InterView VR Client](getting_started/interview_client.md)
- Paper: [InterView: Towards a Unified VR-Capable Interview Environment](publications.md#interview)

## ReEmote

Extends the face and hand tracking of Va.Si.Li-Lab with schema-based emotion mappings that affect both avatars and
their environments, so users and virtual agents share one emotional expression space.
Paper: [ReEmote](publications.md#reemote).

## Va.Si.Li-ES

The event system of Va.Si.Li-Lab. Events are created and serialised at run time and change situational and
environmental parameters (weather, objects, triggers); users are informed and can take part through voting and
feedback. It ships as its own package, see `Assets/Lab/EventSystem/README.md`.
Paper: [Va.Si.Li-ES](publications.md#vasili-es).

## VR-ParlExplorer

A plugin for Va.Si.Li-Lab that virtualises plenary debates: users interact with virtual members of parliament through
LLM-based chatbots. Paper: [VR-ParlExplorer](publications.md#vr-parlexplorer).

## News in Time and Space

News in Time and Space (NiTS) organises news articles (currently based on GDELT) as a geospatial hypertext on a virtual
globe, where users visualise, filter and explore global events and trends.
Paper: [Geo-spatial hypertext in virtual reality](publications.md#news-in-time-and-space).

## Viki LibraRy

A dynamically built library in VR that visualises hypertext systems, with an emphasis on collaborative reading and
spatial recall. Paper: [Viki LibraRy](publications.md#viki-library).

## Semantic Scene Builder

A VR-based text-to-3D framework: scenes are generated from annotated text descriptions and corrected by grabbing and
moving objects instead of editing annotations. Paper: [Semantic Scene Builder](publications.md#semantic-scene-builder).

## FraGA

The Frankfurt VR Gesture–Speech Alignment corpus, used for multimodal analyses of head and hand movements in dialogue.
Paper: [FraGA](publications.md#fraga).
