#! /bin/bash

# create db backup in tmp folder
su - postgres -c pg_dump kattbot > /tmp/kattbot.bak

# move backup file to backups folder
mv /tmp/kattbot.bak /usr/kattbot-db-backups/kattbot-$(date +%Y-%m-%d-%H:%M).bak