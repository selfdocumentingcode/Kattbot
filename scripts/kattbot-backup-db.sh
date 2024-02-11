#! /bin/bash

# create db backup in tmp folder
pg_dump kattbot > /tmp/kattbot.bak

# create folder if it doesnt'exit
mkdir -p $HOME/kattbot-db-backups

# move backup file to backups folder
mv /tmp/kattbot.bak $HOME/kattbot-db-backups/kattbot-$(date +%Y-%m-%d-%H:%M).bak