ffmpeg -y -f vfwcap -r 25 -re -i 0 -filter_complex_script ffmpeg_script.ffmpeg -f mpegts "udp://127.0.0.1:2000"
