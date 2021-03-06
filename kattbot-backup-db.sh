#! /bin/bash

# create db backup in tmp folder
su - postgres -c pg_dump kattbot > /tmp/kattbot.bak

# create folder if it doesnt'exit
mkdir -p /home/kattbot-user/kattbot-db-backups

# move backup file to backups folder
mv /tmp/kattbot.bak /home/kattbot-user/kattbot-db-backups/kattbot-$(date +%Y-%m-%d-%H:%M).bak