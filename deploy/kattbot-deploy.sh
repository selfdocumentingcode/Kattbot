#! /bin/bash

# empty directory
rm -r $HOME/kattbot

# recreate it
mkdir -p $HOME/kattbot

# copy files from staging to app directory
cp -r $HOME/kattbot-staging/. $HOME/kattbot

# copy service file
cp $HOME/kattbot-deploy/kattbot.service $HOME/.config/system/user