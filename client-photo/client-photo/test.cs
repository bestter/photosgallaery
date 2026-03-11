using SixLabors.ImageSharp; using SixLabors.ImageSharp.Metadata.Profiles.Exif; class Test { void M(IExifProfile p) { var v = p.GetValue(ExifTag.DateTimeOriginal); } }
