#! /bin/bash

# remove directory
rm -rf $HOME/kattbot

# recreate it
mkdir -p $HOME/kattbot

# copy files from staging to app directory
cp -r $HOME/kattbot-staging/. $HOME/kattbot

# copy service file and create path if it doesn't exist
cp -p $HOME/kattbot-deploy/kattbot.service $HOME/.config/systemd/user