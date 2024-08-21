# Player Controller
This documentation is work in progress.

``` py title="test.py" linenums="1" hl_lines="2 3"
def bubble_sort(items):
    for i in range(len(items)):
        for j in range(len(items) - 1 - i):
            if items[j] > items[j + 1]:
                items[j], items[j + 1] = items[j + 1], items[j] # (1)
```

1.  I'm a code annotation! I can contain `code`, __formatted
    text__, images, ... basically anything that can be written in Markdown.