#! /bin/bash

# remove directory
rm -rf $HOME/kattbot
mkdir -p $HOME/kattbot

# copy files from staging to app directory
cp -r $HOME/kattbot-staging/. $HOME/kattbot

# copy service file and create path if it doesn't exist
mkdir -p $HOME/.config/systemd/user
cp $HOME/kattbot-deploy/kattbot.service $HOME/.config/systemd/user/