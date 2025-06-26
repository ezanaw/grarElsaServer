# Elsa Server

This project runs an Elsa Workflow server. Configuration values are stored in `appsettings.json` and can be overridden using environment variables.

## API security

The application expects a JWT signing key to be provided via configuration under `Jwt:SigningKey`. For production deployments, store this value in an environment variable (for example `Jwt__SigningKey`) or a secrets store. Never commit real keys to version control.

```json
// appsettings.json
"Jwt": {
  "SigningKey": "CHANGE_ME"
}
```

To run the server locally you can set the variable before starting the application:

```bash
export Jwt__SigningKey="your-secret-key"
```

