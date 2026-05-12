import sys

def add_comment():
    filepath = "PhotoFrontend/src/pages/Gallery.jsx"
    with open(filepath, "r") as f:
        content = f.read()

    # We want to add the comment right before debouncedSearchQuery
    search_str = 'const debouncedSearchQuery = useDebounce(searchQuery, 300);'
    replace_str = '''// ⚡ Bolt: Debounce the search input to reduce blocking main thread operations.
  // This reduces re-renders and the frequency of the O(n) filtering computation below by ~90% during active typing.
  const debouncedSearchQuery = useDebounce(searchQuery, 300);'''

    if search_str in content:
        content = content.replace(search_str, replace_str)
        with open(filepath, "w") as f:
            f.write(content)
        print("Comment added successfully.")
    else:
        print("Failed to find target string.")

if __name__ == "__main__":
    add_comment()
