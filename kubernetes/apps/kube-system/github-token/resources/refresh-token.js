const { App } = require("octokit");
const fs = require("fs");
const https = require("https");

async function refreshToken() {
  const appId = process.env.GITHUB_APP_ID;
  const installationId = process.env.GITHUB_INSTALLATION_ID;
  const privateKey = process.env.GITHUB_PRIVATE_KEY;

  if (!appId || !installationId || !privateKey) {
    throw new Error("Missing required environment variables");
  }

  // Initialize the GitHub App
  const app = new App({
    appId: appId,
    privateKey: privateKey,
  });

  // Generate installation access token
  const octokit = await app.getInstallationOctokit(installationId);
  const token = await octokit.auth();

  if (!token || !token.token) {
    throw new Error("Failed to generate installation access token");
  }

  const accessToken = token.token;
  console.log("Generated installation access token");

  // Read Kubernetes service account token
  const tokenFile = "/var/run/secrets/kubernetes.io/serviceaccount/token";
  const kubeToken = fs.readFileSync(tokenFile, "utf8");
  const caCertFile = "/var/run/secrets/kubernetes.io/serviceaccount/ca.crt";

  const namespace = process.env.NAMESPACE || "flux-system";
  const secretName = "github-token";

  // Patch the Kubernetes secret using the API
  const patchData = {
    metadata: {
      annotations: {
        "reloader.stakater.com/auto": "true",
        "reflector.v1.k8s.emberstack.com/reflection-allowed": "true",
        "reflector.v1.k8s.emberstack.com/reflection-auto-enabled": "true",
      },
    },
    data: {
      token: Buffer.from(accessToken).toString("base64"),
    },
  };

  const options = {
    hostname: "kubernetes.default.svc.cluster.local",
    path: "/api/v1/namespaces/" + namespace + "/secrets/" + secretName,
    method: "PATCH",
    headers: {
      Authorization: "Bearer " + kubeToken,
      "Content-Type": "application/merge-patch+json",
    },
    ca: fs.readFileSync(caCertFile),
  };

  return new Promise((resolve, reject) => {
    const req = https.request(options, (res) => {
      let data = "";
      res.on("data", (chunk) => {
        data += chunk;
      });
      res.on("end", () => {
        if (res.statusCode >= 400) {
          reject(new Error("HTTP " + res.statusCode + ": " + data));
        } else {
          console.log("Successfully updated GitHub token secret");
          resolve();
        }
      });
    });

    req.on("error", reject);
    req.write(JSON.stringify(patchData));
    req.end();
  });
}

refreshToken().catch((error) => {
  console.error("Error refreshing token:", error);
  process.exit(1);
});
