# disaster-platform-web

Integrated execution uses `Api:UseMock=false` and sends all domain requests through `Api:GatewayUrl`, including Eden-backed modules. Mock preview is restricted to Development.

Configure `Security:Authority` (HTTPS Keycloak realm), `Security:ClientId=razor-web`, `Security:ClientSecret` and `Security:DataProtectionPath` externally. The key directory must be private, persistent and shared by presentation replicas. Never commit secrets or key-ring files. Configure the trusted issuer CA through the deployment environment. Register `/signin-oidc` and `/signout-callback-oidc` with the client's exact HTTPS origin.

Login uses Authorization Code, PKCE and the issuer's MFA flow. Access tokens stay in encrypted HttpOnly/Secure authentication cookies and are forwarded only to configured APIs. Session expiration follows the access token; expired sessions obtain a new authorization through Keycloak. Backend organization, disaster and resource authorization remains authoritative. The existing logout action signs out locally and at the issuer.

Directed validation: `python3 scripts/test_oidc.py` requires requests/BeautifulSoup, `RAZOR_TEST_URL` (local HTTPS), `RAZOR_TEST_CA`, `RAZOR_TEST_IDENTITY` (protected JSON with username/password/otpSecret for an enrolled synthetic MFA user) and `RAZOR_TEST_ORGANIZATION_NAME`. No administrative credential is used by the test.

Local execution, protected profiles, ports and the connected lifecycle: [LOCAL_RUN.md](LOCAL_RUN.md). Domain clients use explicit routes and typed commands; unsupported proposed contracts are disabled, with Eden native operations remaining in Eden.

Targeted local regressions: `dotnet run --file disaster-platform-presentation/scripts/contracts_test.cs` from the organization root checks date precision, multipart streams/SHA-256 and directory independence. These are unit checks; real MVC/API/Storage validation is a separate environment-dependent step.
