#!/bin/bash
set -euo pipefail
echo "Starting 1Password user fetch..."

# Set default OUTPUT_DIR if not provided
OUTPUT_DIR="${OUTPUT_DIR:-/tmp/lldap-sync}"

# Validate required environment variables
if [ -z "$TAILSCALE_DOMAIN" ]; then
    echo "Error: TAILSCALE_DOMAIN environment variable not set"
    exit 1
fi

if [ -z "$OP_CONNECT_HOST" ]; then
    echo "Error: OP_CONNECT_HOST environment variable not set"
    exit 1
fi

if [ -z "$OP_CONNECT_TOKEN" ]; then
    echo "Error: OP_CONNECT_TOKEN environment variable not set"
    exit 1
fi

# Create output directories
mkdir -p "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR/groups"
mkdir -p "$OUTPUT_DIR/users"

# Function to make authenticated requests to 1Password Connect API
op_connect_request() {
    local method="$1"
    local endpoint="$2"
    local data="${3:-}"

    local curl_args=(
        -s
        -X "$method"
        -H "Authorization: Bearer $OP_CONNECT_TOKEN"
        -H "Content-Type: application/json"
    )

    if [ -n "$data" ]; then
        curl_args+=(-d "$data")
    fi

    curl "${curl_args[@]}" "$OP_CONNECT_HOST$endpoint"
}

# Get all vaults to find items
echo "Fetching vaults from 1Password Connect..."
vaults=$(op_connect_request "GET" "/v1/vaults")

if [ -z "$vaults" ] || [ "$vaults" = "[]" ]; then
    echo "No vaults found"
    exit 0
fi

echo "Found $(echo "$vaults" | jq length) vaults"

# Search for items with the specified tag across all vaults
user_tag="$TAILSCALE_DOMAIN/user"
echo "Searching for items with tag '$user_tag' across all vaults..."

# Instead of a Bash array, use a JSON file to store items
all_items_file=$(mktemp)
echo "[]" >"$all_items_file"
vault_count=0

while IFS= read -r vault_id; do
    echo "Searching vault: $vault_id"

    # Use SCIM filter to find items with the specific tag
    filter="tag eq \"$user_tag\""
    encoded_filter=$(printf '%s' "$filter" | jq -sRr @uri)

    vault_items=$(op_connect_request "GET" "/v1/vaults/$vault_id/items?filter=$encoded_filter")

    if [ -n "$vault_items" ] && [ "$vault_items" != "[]" ]; then
        item_count=$(echo "$vault_items" | jq length)
        echo "Found $item_count items with tag '$user_tag' in vault $vault_id"

        # Extract item IDs and append to our JSON array
        vault_item_ids=$(echo "$vault_items" | jq -r '.[].id')
        if [ -n "$vault_item_ids" ]; then
            while IFS= read -r item_id; do
                if [ -n "$item_id" ]; then
                    # Add item ID to our JSON array
                    jq --arg id "$item_id" '. += [$id]' "$all_items_file" > "${all_items_file}.tmp" && mv "${all_items_file}.tmp" "$all_items_file"
                fi
            done <<< "$vault_item_ids"
        fi
    fi

    ((vault_count++))
done < <(echo "$vaults" | jq -r '.[].id')

# Check if we have any items
item_count=$(jq 'length' "$all_items_file")
if [ "$item_count" -eq 0 ]; then
    echo "No users found with tag '$user_tag'"
    # Create empty directory but no files - bootstrap.sh will handle empty configs
    rm "$all_items_file"
    exit 0
fi

echo "Found $item_count user items total"

# Initialize array to collect all unique groups
declare -A all_groups
all_groups["user"]=1 # Always include the default group

# Process each item to extract user details
user_count=0

# Use jq to iterate through the IDs
while IFS= read -r item_id; do
    echo "Processing item: $item_id"

    # First, we need to find which vault this item belongs to
    # We'll search through all vaults to find the item
    item_details=""

    for vault_id in $(echo "$vaults" | jq -r '.[].id'); do
        # Try to get the item from this vault
        temp_details=$(op_connect_request "GET" "/v1/vaults/$vault_id/items/$item_id" 2>/dev/null || echo "")
        if [ -n "$temp_details" ] && [ "$temp_details" != "null" ] && [ "$temp_details" != "" ]; then
            # Verify the response is valid JSON
            if echo "$temp_details" | jq . >/dev/null 2>&1; then
                item_details="$temp_details"
                echo "Found item details in vault: $vault_id"
                break
            fi
        fi
    done

    if [ -z "$item_details" ] || [ "$item_details" = "null" ]; then
        echo "Warning: Could not retrieve details for item $item_id"
        continue
    fi

    echo "Extracting fields for item: $item_id"

    # Extract fields from the item details
    username=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "username" or .label == "Username") | .value // empty')
    password=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "password" or .label == "Password") | .value // empty')
    email=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "email" or .label == "Email") | .value // empty')
    display_name=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "display_name" or .label == "Display Name") | .value // empty')
    first_name=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "first_name" or .label == "First Name") | .value // empty')
    last_name=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "last_name" or .label == "Last Name") | .value // empty')
    groups=$(echo "$item_details" | jq -r '.fields[]? | select(.label == "groups" or .label == "Groups") | .value // empty')

    echo "Extracted fields - username: $username, email: $email, display_name: $display_name"

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
        echo "Warning: Skipping item $item_id - missing email"
        continue
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
        # Clean up the groups string and convert to JSON array
        clean_groups=$(echo "$groups" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
        if [ -n "$clean_groups" ]; then
            groups_array=$(echo "$clean_groups" | jq -R 'split(",") | map(select(length > 0) | ltrimstr(" ") | rtrimstr(" ")) | select(length > 0)')
        else
            groups_array='["lldap_password_manager"]'
        fi
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
    else
        # Add default group
        all_groups["lldap_password_manager"]=1
    fi

    # Create user config in bootstrap.sh format
    if ! user_config=$(jq -n \
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
        }'); then
        echo "Error: Failed to create user config for $username"
        continue
    fi

    # Write individual user config file
    user_file="$OUTPUT_DIR/users/$username.json"
    if ! echo "$user_config" >"$user_file"; then
        echo "Error: Failed to write user config file $user_file"
        continue
    fi
    echo "Created user config: $user_file"

    ((user_count++))

done < <(jq -r '.[]' "$all_items_file")

# Clean up the temporary file
rm "$all_items_file"

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
    group_file="$OUTPUT_DIR/groups/$group_name.json"
    echo "$group_config" >"$group_file"
    echo "Created group config: $group_file"

    ((group_count++))
done

echo "Successfully created $group_count group config files in $OUTPUT_DIR"
echo "User sync preparation completed successfully"

echo "Starting LLDAP user sync using bootstrap.sh..."

# Check if bootstrap.sh exists
if [ ! -f "/app/bootstrap.sh" ]; then
    echo "Error: bootstrap.sh not found at /app/bootstrap.sh"
    echo "This script requires the LLDAP bootstrap.sh script to be available"
    exit 1
fi

# Run the bootstrap script
echo "Running LLDAP bootstrap.sh..."
if /app/bootstrap.sh; then
    echo "User sync completed successfully"
else
    echo "User sync failed"
    exit 1
fi
