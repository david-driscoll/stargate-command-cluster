# Component

- This is a postgres component.
- It's presence as part of a ks.yaml file will trigger the update script `Update.cs` in this directory, which in turn will:
    - generate a password for the database (if missing)
    - add a document to kubernetes/apps/database/postgres/app/passwords.sops.yaml
    - add an external secret into kubernetes/apps/database/postgres/app/users.yaml which adds the secret into the database namespace.
    - adds a push secret to kubernetes/apps/database/postgres/postgres-push-secrets/push-secrets.yaml
