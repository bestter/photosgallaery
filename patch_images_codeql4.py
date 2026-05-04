import re

with open("PhotoAppApi/Controllers/ImagesController.cs", "r") as f:
    content = f.read()

target1 = 'if (string.IsNullOrEmpty(fileName) || fileName.Contains("/") || fileName.Contains("\\\\") || fileName.Contains("..")) return BadRequest("Invalid file name.");\n            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return BadRequest("Invalid file name.");\n            var safeFileName = Path.GetFileName(fileName);'

replacement1 = """if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return BadRequest("Invalid file name.");

            // Explicitly normalize path separators for cross-platform safety
            var safeFileName = Path.GetFileName(fileName.Replace("\\\\", "/"));

            // Prevent basic traversal attempts
            if (fileName != safeFileName) return BadRequest("Invalid file name.");"""

content = content.replace(target1, replacement1)

with open("PhotoAppApi/Controllers/ImagesController.cs", "w") as f:
    f.write(content)
