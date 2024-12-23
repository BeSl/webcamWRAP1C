#Запись камеры из 1с
Проект взаимодействия 1с с веб-камерой, сетевыми камерами по протоколу rtsp
позволяет выполнять просмотр, запись видеопотока из 1с, вызов доступен в управляемых формах и обычных 

# **Пример подключения веб-камеры (должен заработать и RTSP поток) из 1С.**
Выполняется сборка dll, регистрация в системе

__регистрировать так:__
перейти в каталог с библиотекой и выполнить(от админа)
```ps
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe bslDMReader.dll /codebase
```
000000296



 rtsp://admin:Ab123456@10.0.109.128:554/Streaming/Channels/101/
 rtsp://admin:Ab123456@10.0.109.128:554/mpeg4/ch01/main/av_stream
 rtsp://admin:Ab123456@10.0.109.128:554/Streaming/Channels/101



rtsp://admin:Ab123456@10.0.109.128/

rtsp://admin:Ab123456@10.0.109.128:554/isapi/Streaming/Channels/101


RTSP://admin:Ab123456@10.0.109.128:554/isapi/Streaming/Channels/2


RTSP://admin:Ab123456@10.0.109.128:554/1

 $ vlc screen:// --screen-fps=25 --sout="#transcode{vcodec=mp4v}:std{access=http, mux=mkv, dst=:8090/stream}"

vlc --width 400 --height 200 RTSP://admin:Ab123456@10.0.109.128:554/1 

vlc --width 400 --height 200 "dshow:// :dshow-vdev="A4tech HD 720P PC Camera"

vlc --width=640 --height=480 RTSP://admin:Ab123456@10.0.109.128:554/1  --sout "#transcode{vcodec=mp4v,scale=1,acodec=none,soverlay}:duplicate{dst=display,dst=std{access=file,mux=mp4,dst='F:\\записиКамеры\\rec_2024_1.p4'}}" 
--video-filter "scene" --scene-ratio=1 --scene-prefix=snapshot --no-sout-audio --sub-filter "marq" --marq-position=6 --marq-size=24 --marq-timeout=0


vlc RTSP://admin:Ab123456@10.0.109.128:554/1  --sout "#transcode{vcodec=mp4v,vfilter=canvas{width=1920,height=1080}}:duplicate{dst=display,dst=std{access=file,mux=mp4,dst='F:\\записиКамеры\\rec_2024_21.mp4'}}" 
