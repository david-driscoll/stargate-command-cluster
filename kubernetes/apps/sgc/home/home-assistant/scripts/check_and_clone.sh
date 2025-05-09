#!/bin/bash

# Check if the /config/.git directory exists
if [ ! -d "/config/.git" ]; then
    echo "Git directory not found. Cloning repository..."
    git clone --bare ${HOME_ASSISTANT_SSH_URL} /config/.git
else
    echo "Git directory exists. Hydrating master branch..."
    git --git-dir=/config/.git --work-tree=/config checkout master
fi
