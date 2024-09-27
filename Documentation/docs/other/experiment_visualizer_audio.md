# Experiment Visualization Audio

## Introduction

This tool enables the transcription of voice recordings made of participants in experiments. A sentiment analysis is automatically included in the visualisation. Additionally, users may highlight tags that they have defined. Each turn can be analysed by a LLM, and the prompt used can be customised. There is a default prompt with predefined types of turns. Furthermore, users can generate a wordcloud of the transcription of the selected scenario.

## Setup

### Requirements

- Python 3.12.2
- login.json in directory ../db/login/login.json (Login data for MongoDB)

#### Packages
- streamlit (1.38.0) [(more information)](https://docs.streamlit.io/get-started/installation)
- pymongo (4.8.0) [(more information)](https://pymongo.readthedocs.io/en/stable/installation.html)
- pydub (0.25.1) [(more information)](https://github.com/jiaaro/pydub)
- numpy (1.26.4) [(more information)](https://numpy.org/install/)
- openai-whisper (20231117) [(more information)](https://github.com/openai/whisper)
- spacy (3.7.6) [(more information)](https://spacy.io/usage)
- wordcloud (1.9.3) [(more information)](https://github.com/amueller/word_cloud)
- matplotlib (3.9.2) [(more information)](https://matplotlib.org/stable/)
- textblob (0.18.0.post0) [(more information)](https://textblob.readthedocs.io/en/dev/)
- transformers (4.44.2) [(more information)](https://github.com/huggingface/transformers)
- torch (2.4.0) [(more information)](https://pytorch.org/)

### Getting started

To start enter "streamlit run Live_Experiment_Analysis.py" in terminal.
  
## Pages

### Audiodata Analysis

#### Load Data

On this page you can initialize the transcription and see the visualisation.

The first step is to select the scenario and the date from which the voice recordings are to be analyzed.
This tool uses Whisper to transcribe the audio data. There are different modelsizes of whisper available. Therefore, in the next step, select one of the modelsizes from the dropdown menu.
Once you have selected the scenario, date and model size, press the button **load data** to 

1. Export documents from the MongoDB "Audio" database with the selected scene and date.
2. Convert the audio data into a .wav file and export it.
3. Transcribe the audio files using the whisper model of your choice.

The button **load turntaking analysis** allows you to load a turntaking analysis using the llm "NousResearch/Llama-2-7b-chat-hf" from [Huggingface](https://huggingface.co/NousResearch/Llama-2-7b-chat-hf). The prompt that the LLM takes as input can be edited in the [Settings](#edit-prompt-for-turntaking-analysis).

If you have previously loaded the turntaking analysis and wish to reload it (e.g. after editing the prompt), you can press the **reload turntaking analysis** button. 

#### Wordcloud

By pressing the button **show wordcloud**, you can create a wordcloud of the full transcription of the scenario. Words contained in the german_stopwords_full.txt file are excluded.

#### Show Transcription and Turntaking Analysis

Use the slider bar to set the interval at which the transcription will be displayed.

You have the option to highlight personalised tags in the transcription. Tags can be added or deleted in the [Settings](#set-tags). To highlight your tags, make sure the corresponding checkbox is selected.

Finally, you can visualise the voice recordings as a messenger by clicking on either **show transcription** or **show transcription with turntaking analysis**. To view the transcription with a turntaking analysis, you must first click on **load turntaking analysis**.

The visualisation of the speech recordings also includes a sentiment analysis. The sentiment of each sentence is colour coded in five categories: negative (red), tendence negative (orange), neutral (grey), tendence positive (blue) and positive (green). The thresholds between each category can be adjusted in the [Settings](#set-thresholds-for-sentiment-analysis).

### Settings

This is the page where you can personalise the features of this tool according to your preferences.

#### Set Tags

Tags can be added using the input field on the left and removed with the input field on the right. The tags currently defined are displayed below the input fields. All of the tags that are shown below *current tags* will be highlighted if you select the corresponding option in the [Audiodata Analysis](#audiodata-analysis).

#### Set Thresholds for Sentiment Analysis

This tool allows you to view five different levels of sentiment: negative, tendence negative, neutral, tendence positive and positive. Each level represents an interval within the range (-1, 1). The value of the sentiment is within the interval indicated by the color of the transcription.

- a is the threshold between "negative" and "tendence negative"
- b is the threshold between "tendence negative" and "neutral"
- c is the threshold between "neutral" and "tendence positive"
- d is the threshold between "tendence positive" and "positive"

Note that the parameters should be selected according to a < b < c < d. Also, no parameter should be selected the same as another.
If you reload the "Settings" page, the thresholds will be set to te default values (a=-0.2, b=-0.01, c=0.01, d=0.2).

#### Edit Prompt for Turntaking Analysis

To analyze the turns of the speech recordings, the LLM takes a prompt describing the types of turns.
The prompt consists of two parts: one is editable and the other is not.

The first part of the prompt can be edited using the input field *customize prompt*.

There is a default prompt which you can see below *see full default prompt here* when you first open the "Settings" page.

The input field is not expandable. If you want to edit a part of the default prompt, it is recommended that you copy the default prompt and edit it externally in a text editor of your choice. 
You can then press *empty prompt* and paste your edited prompt into the input field. Then press Enter to submit the updated prompt.
To return to the default prompt, press *set prompt to default*.

The following part of the prompt cannot be edited. This is to ensure, that the two parts of a turn are placed correctly within the prompt.

```
<input>"Turn: Person X says: {first part of the turn} Person Y responds to Person X: {second part of the turn}"<\input>
```
