ffmpeg -re -i /dev/video0 -filter_complex_script ffmpeg_script.ffmpeg -c:v libx264 -f mpegts "udp://127.0.0.1:2000"

