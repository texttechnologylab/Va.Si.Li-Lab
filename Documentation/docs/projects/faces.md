# New Data Spaces

[New Data Spaces](https://www.new-data-spaces.de/en-us/) - DFG SPP 2431

![New Data Spaces](../images/projects/NewDataSpaces.png){loading=lazy}

## FACES

[FACES](https://gepris.dfg.de/gepris/projekt/539621548?language=en) (Feasibility,
acceptance, and data quality of new multimodal surveys) is part of this SPP.

Conventional surveys record the answer and discard everything that produced it -
the hesitation, the gaze, whether the question was understood at all. FACES asks
whether conducting interviews in VR can retain that context, and whether the
resulting data holds up: is a VR-mediated interview *feasible*, do respondents
*accept* it, and is the *data quality* comparable to established modes?

## InterView

The platform built to answer that question is **InterView**. A participant wears
a VR headset and sits across from the interviewer's avatar in a shared virtual
room; the interviewer works from an ordinary web browser. Speech, gaze, posture,
hand movement, facial expression and the answers themselves are all captured on a
shared clock.

InterView is built on Va.Si.Li-Lab and adds a WebRTC media layer, a browser-based
interviewer console, and a token-driven session model.

!!! info "InterView has its own documentation"
    Architecture, the full deployment guide, and reference documentation for
    every service - Janus, coturn, Ubiq, the logging API and the web client - are
    at **[texttechnologylab.github.io/InterView](https://texttechnologylab.github.io/InterView/)**.

    Setting up the **VR client** is documented here, in
    [InterView VR Client](../getting_started/interview_client.md).

| Repository | Contains |
|---|---|
| [InterView](https://github.com/texttechnologylab/InterView) | Deployment stack and platform documentation |
| [Va.Si.Li-Lab](https://github.com/texttechnologylab/Va.Si.Li-Lab) | The VR client |
| [faces-website](https://github.com/texttechnologylab/faces-website) | Web client |
| [Va.Si.Li-Lab-backend](https://github.com/texttechnologylab/Va.Si.Li-Lab-backend) | Ubiq server and logging API sources |
