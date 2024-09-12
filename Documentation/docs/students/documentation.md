# Writing Documentation
## General
This documentation is based on the [MkDocs-Material](https://squidfunk.github.io/mkdocs-material/reference/) framework. 
It is written in [Markdown](https://www.markdownguide.org/basic-syntax/) and can be found in the `Documentation/docs` folder. 


## Testing locally
### Requirements
```bash
conda create -n mkdocs python=3.12
conda activate mkdocs
pip install mkdocs-material 
pip install mkdocs-git-revision-date-localized-plugin mkdocs-git-committers-plugin-2 mkdocs-git-authors-plugin mkdocs-glightbox
```

### Run

```bash
cd Documentation
mkdocs serve
```


## Adding a new page
To add a new page, create a new markdown file in the `Documentation/docs` folder.
Then insert the file path in the `mkdocs.yml` file under the `nav` section.
