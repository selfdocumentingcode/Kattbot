#! /bin/bash

# empty directory
rm -r /usr/kattbot

# recreate it
mkdir -p /usr/kattbot

# copy files from deploy to app directory
cp -r /usr/kattbot-deploy/github/workspace/deploy/. /usr/kattbot