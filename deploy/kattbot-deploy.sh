#! /bin/bash

# empty directory
rm -r $HOME/kattbot

# recreate it
mkdir -p $HOME/kattbot

# copy files from staging to app directory
cp -r $HOME/kattbot-staging/github/workspace/publish-output/. $HOME/kattbot

# 