# Writing Documentation
## General
This documentation is based on the [MkDocs-Material](https://squidfunk.github.io/mkdocs-material/reference/) framework. 
It is written in [Markdown](https://www.markdownguide.org/basic-syntax/) and can be found in the `Documentation/docs` folder. 


## Testing locally
With [uv](https://docs.astral.sh/uv/getting-started/installation/) installed, no further setup is needed. From the
repository root:

```bash
cd Documentation
uvx --with mkdocs-material --with mkdocs-git-revision-date-localized-plugin --with mkdocs-glightbox mkdocs serve
```

Then open <http://127.0.0.1:8000>. Pages reload when you save.

Before opening a pull request, check that the docs build without warnings, as the CI does:

```bash
uvx --with mkdocs-material --with mkdocs-git-revision-date-localized-plugin --with mkdocs-glightbox mkdocs build --strict
```

Without uv, install the same packages with pip (`pip install mkdocs-material mkdocs-git-revision-date-localized-plugin
mkdocs-glightbox`) and run `mkdocs serve`.


## Adding a new page
To add a new page, create a new markdown file in the `Documentation/docs` folder.
Then insert the file path in the `mkdocs.yml` file under the `nav` section.

## Publishing
The docs are built and published to GitHub Pages on every push to `main` and on every pull request, so a pull
request shows up on the live site before it is merged.
