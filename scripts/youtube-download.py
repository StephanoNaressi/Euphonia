import json
import os
import subprocess
import sys

data = {}

with open('../data/info.json', 'r', encoding='utf-8') as fd:
    data = json.load(fd)

name = input("Enter the song name: ")
if any(x["name"] == name for x in data["musics"]):
    print("There is always a song with that name")
    exit(1)

url = input("Enter the YouTube URL: ")
if any(x["youtube"] == url for x in data["musics"]):
    print("There is already a song with the same URL")
    exit(1)

if not os.path.isdir("tmp"):
    os.mkdir("tmp")
subprocess.run(["youtube-dl", url, "-o", "tmp/" + name], stderr=sys.stderr, stdout=sys.stdout)
newName = subprocess.check_output(['ls', '"tmp/' + name + '".*'])
subprocess.run(["ffmpeg", "-i", newName, "../data/" + name + ".wav"], capture_output=True)
subprocess.run(["rm", '"tmp/' + name + '".*'], capture_output=True)