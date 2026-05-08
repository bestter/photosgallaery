import re

with open("PhotoAppApi/Controllers/PhotosController.cs", "r") as f:
    content = f.read()

# The code we want to replace:
#                 using (var sha512 = SHA512.Create())
#                 {
#                     // 2. Boucler sur chaque photo
#                     foreach (var photo in photosSansHash)
#                     {
#                         var safeFileName = Path.GetFileName(photo.FileName.Replace("\\", "/"));
#                         var filePath = Path.Combine(rootPath, "images", safeFileName);
#
#                         // 3. Vérifier si le fichier physique existe toujours
#                         if (System.IO.File.Exists(filePath))
#                         {
#                             // Calculer le hash
#                             using (var stream = System.IO.File.OpenRead(filePath))
#                             {
#                                 var hashBytes = await sha512.ComputeHashAsync(stream);
#                                 photo.FileHash = Convert.ToHexStringLower(hashBytes);
#                             }
#                             updatedCount++;
#                         }
#                         else
#                         {
#                             missingFilesCount++;
#                             _logger.Warn($"Fichier introuvable pour la photo ID {photo.Id} : {filePath}");
#                         }
#                     }
#                 }

old_code = """                using (var sha512 = SHA512.Create())
                {
                    // 2. Boucler sur chaque photo
                    foreach (var photo in photosSansHash)
                    {
                        var safeFileName = Path.GetFileName(photo.FileName.Replace("\\\\", "/"));
                        var filePath = Path.Combine(rootPath, "images", safeFileName);

                        // 3. Vérifier si le fichier physique existe toujours
                        if (System.IO.File.Exists(filePath))
                        {
                            // Calculer le hash
                            using (var stream = System.IO.File.OpenRead(filePath))
                            {
                                var hashBytes = await sha512.ComputeHashAsync(stream);
                                photo.FileHash = Convert.ToHexStringLower(hashBytes);
                            }
                            updatedCount++;
                        }
                        else
                        {
                            missingFilesCount++;
                            _logger.Warn($"Fichier introuvable pour la photo ID {photo.Id} : {filePath}");
                        }
                    }
                }"""

new_code = """                // 2. Boucler sur chaque photo (Optimized for concurrent hashing)
                // ⚡ Bolt: Execute hashing concurrently to minimize stream I/O latency
                var fileTasks = photosSansHash.Select(photo => Task.Run(async () =>
                {
                    var safeFileName = Path.GetFileName(photo.FileName.Replace("\\\\", "/"));
                    var filePath = Path.Combine(rootPath, "images", safeFileName);

                    // 3. Vérifier si le fichier physique existe toujours
                    if (System.IO.File.Exists(filePath))
                    {
                        // Calculer le hash
                        using var stream = System.IO.File.OpenRead(filePath);
                        using var sha512 = SHA512.Create();
                        var hashBytes = await sha512.ComputeHashAsync(stream);
                        return (Photo: photo, Hash: Convert.ToHexStringLower(hashBytes), Exists: true, FilePath: filePath);
                    }
                    else
                    {
                        return (Photo: photo, Hash: (string?)null, Exists: false, FilePath: filePath);
                    }
                }));

                var results = await Task.WhenAll(fileTasks);

                foreach (var result in results)
                {
                    if (result.Exists)
                    {
                        result.Photo.FileHash = result.Hash;
                        updatedCount++;
                    }
                    else
                    {
                        missingFilesCount++;
                        _logger.Warn($"Fichier introuvable pour la photo ID {result.Photo.Id} : {result.FilePath}");
                    }
                }"""

content = content.replace(old_code, new_code)

with open("PhotoAppApi/Controllers/PhotosController.cs", "w") as f:
    f.write(content)
