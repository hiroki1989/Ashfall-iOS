param([string]$Evidence='D:/Unity/AshfallArtValidation/MotionFinal')
$ffmpeg='C:/Users/hiroki/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg.Shared_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0.1-full_build-shared/bin/ffmpeg.exe'
# 32 frames / (4 metres / 7 metres per second) = 56 fps, matching runtime cadence.
& $ffmpeg -hide_banner -loglevel error -y -stream_loop 7 -framerate 56 -i "$Evidence/motion-focus/12-0-carry/frame-%03d.png" -stream_loop 7 -framerate 56 -i "$Evidence/motion-focus/12-4-carry/frame-%03d.png" -stream_loop 7 -framerate 56 -i "$Evidence/motion-focus/14-7-aim/frame-%03d.png" -filter_complex '[0:v]crop=iw/2:ih:0:0[a];[1:v]crop=iw/2:ih:0:0[b];[2:v]crop=iw/2:ih:0:0[c];[a][b][c]hstack=inputs=3[v]' -map '[v]' -c:v libx264 -pix_fmt yuv420p "$Evidence/motion-review.mp4"
if($LASTEXITCODE -ne 0){throw 'Motion review encoding failed'}
