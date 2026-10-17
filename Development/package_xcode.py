import zipfile,pathlib
root=pathlib.Path('D:/Unity/Ashfall-iOS/Builds/iOS/Xcode')
out=pathlib.Path('D:/Unity/Ashfall-iOS/Builds/Ashfall-Xcode.zip')
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=3,allowZip64=True) as z:
 for f in root.rglob('*'):
  if f.is_file():
   if f.suffix.lower() in ('.p12','.p8','.mobileprovision','.key'): raise RuntimeError('Credential unexpectedly in source export')
   z.write(f,f.relative_to(root).as_posix())
print('Xcode zip bytes:',out.stat().st_size)
