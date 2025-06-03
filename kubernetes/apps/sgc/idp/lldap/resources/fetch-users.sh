#!/bin/bash
set -euo pipefail

echo "Starting 1Password user fetch..."

# Default values - these should be set by the environment
OUTPUT_DIR="${OUTPUT_DIR:-/shared/user-configs}"
GROUP_CONFIGS_DIR="${GROUP_CONFIGS_DIR:-/shared/group-configs}"
TAILSCALE_DOMAIN="${TAILSCALE_DOMAIN:-opossum-yo.ts.net}"
LLDAP_LDAP_BASE_DN="${LLDAP_LDAP_BASE_DN:-dc=example,dc=com}"

# Validate required environment variables
if [ -z "$TAILSCALE_DOMAIN" ]; then
    echo "Error: TAILSCALE_DOMAIN environment variable not set"
    exit 1
fi

if [ -z "$LLDAP_LDAP_BASE_DN" ]; then
    echo "Error: LLDAP_LDAP_BASE_DN environment variable not set"
    exit 1
fi

# Create output directories
mkdir -p "$OUTPUT_DIR"
mkdir -p "$GROUP_CONFIGS_DIR"

# Check if op is authenticated
if ! op whoami >/dev/null 2>&1; then
    echo "Error: Not authenticated with 1Password"
    exit 1
fi

# Fetch all items with the tag
echo "Fetching users from 1Password with tag '${TAILSCALE_DOMAIN}/user'..."

# Get all items with the specified tag
items=$(op item list --tags "${TAILSCALE_DOMAIN}/user" --format json)

if [ -z "$items" ] || [ "$items" = "[]" ]; then
    echo "No users found with tag '${TAILSCALE_DOMAIN}/user'"
    # Create empty directory but no files - bootstrap.sh will handle empty configs
    exit 0
fi

echo "Found $(echo "$items" | jq length) user items"

# Initialize array to collect all unique groups
declare -A all_groups
all_groups["user"]=1 # Always include the default group

# Process each item to extract user details
user_count=0

while IFS= read -r item_id; do
    echo "Processing item: $item_id"

    # Get item details
    item_details=$(op item get "$item_id" --format json)

    # Extract fields
    username=$(echo "$item_details" | jq -r '.fields[] | select(.label == "username" or .label == "Username") | .value // empty')
    password=$(echo "$item_details" | jq -r '.fields[] | select(.label == "password" or .label == "Password") | .value // empty')
    email=$(echo "$item_details" | jq -r '.fields[] | select(.label == "email" or .label == "Email") | .value // empty')
    display_name=$(echo "$item_details" | jq -r '.fields[] | select(.label == "display_name" or .label == "Display Name") | .value // empty')
    first_name=$(echo "$item_details" | jq -r '.fields[] | select(.label == "first_name" or .label == "First Name") | .value // empty')
    last_name=$(echo "$item_details" | jq -r '.fields[] | select(.label == "last_name" or .label == "Last Name") | .value // empty')
    groups=$(echo "$item_details" | jq -r '.fields[] | select(.label == "groups" or .label == "Groups") | .value // empty')

    # Use title as display name if display_name is empty
    if [ -z "$display_name" ]; then
        display_name=$(echo "$item_details" | jq -r '.title // empty')
    fi

    # Validate required fields
    if [ -z "$username" ] || [ -z "$password" ]; then
        echo "Warning: Skipping item $item_id - missing username or password"
        continue
    fi

    # Default email if not provided
    if [ -z "$email" ]; then
        email="${username}@${LLDAP_LDAP_BASE_DN#dc=}"
        email="${email//,dc=/.}"
    fi

    # Parse display name into first and last name if not provided separately
    if [ -z "$first_name" ] && [ -n "$display_name" ]; then
        first_name=$(echo "$display_name" | cut -d' ' -f1)
        if [ "$display_name" != "$first_name" ]; then
            last_name=$(echo "$display_name" | cut -d' ' -f2-)
        fi
    fi

    # Default names if still empty
    if [ -z "$first_name" ]; then
        first_name="$username"
    fi
    if [ -z "$last_name" ]; then
        last_name=""
    fi
    if [ -z "$display_name" ]; then
        if [ -n "$last_name" ]; then
            display_name="$first_name $last_name"
        else
            display_name="$first_name"
        fi
    fi

    # Default groups if not provided (convert comma-separated to array)
    if [ -z "$groups" ]; then
        groups_array='["lldap_password_manager"]'
    else
        groups_array=$(echo "$groups" | jq -R 'split(",") | map(select(length > 0) | ltrimstr(" ") | rtrimstr(" "))')
    fi

    # Collect groups for group config generation
    if [ -n "$groups" ]; then
        # Parse comma-separated groups and add to all_groups
        IFS=',' read -ra group_list <<<"$groups"
        for group in "${group_list[@]}"; do
            # Trim whitespace
            group=$(echo "$group" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
            if [ -n "$group" ]; then
                all_groups["$group"]=1
            fi
        done
    fi

    # Create user config in bootstrap.sh format
    user_config=$(jq -n \
        --arg id "$username" \
        --arg email "$email" \
        --arg password "$password" \
        --arg displayName "$display_name" \
        --arg firstName "$first_name" \
        --arg lastName "$last_name" \
        --argjson groups "$groups_array" \
        '{
            id: $id,
            email: $email,
            password: $password,
            displayName: $displayName,
            firstName: $firstName,
            lastName: $lastName,
            groups: $groups
        }')

    # Write individual user config file
    user_file="$OUTPUT_DIR/user-$(printf "%03d" $user_count)-$username.json"
    echo "$user_config" >"$user_file"
    echo "Created user config: $user_file"

    ((user_count++))

done < <(echo "$items" | jq -r '.[].id')

echo "Successfully created $user_count user config files in $OUTPUT_DIR"

# Generate group configuration files
echo "Generating group configuration files..."
group_count=0

for group_name in "${!all_groups[@]}"; do
    # Create group config in bootstrap.sh format
    group_config=$(jq -n \
        --arg name "$group_name" \
        '{
            name: $name
        }')

    # Write group config file
    group_file="$GROUP_CONFIGS_DIR/group-$(printf "%03d" $group_count)-$group_name.json"
    echo "$group_config" >"$group_file"
    echo "Created group config: $group_file"

    ((group_count++))
done

echo "Successfully created $group_count group config files in $GROUP_CONFIGS_DIR"
echo "User sync preparation completed successfully"
