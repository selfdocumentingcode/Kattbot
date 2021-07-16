#! /bin/bash

# empty directory
rm -r /home/kattbot-user/kattbot

# recreate it
mkdir -p /home/kattbot-user/kattbot

# copy files from deploy to app directory
cp -r /home/kattbot-user/kattbot-deploy/github/workspace/deploy/. /home/kattbot-user/kattbot