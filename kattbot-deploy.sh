#! /bin/bash

# create folder if it doesnt'exit
mkdir -p /usr/kattbot

# empty directory
rm -r /usr/kattbot

# copy files from deploy to app directory
cp -r /usr/kattbot-deploy/github/workspace/deploy/. /usr/kattbot